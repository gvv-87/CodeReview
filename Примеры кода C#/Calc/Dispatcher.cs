using System;

namespace Aggregator.Calc
{
    public sealed class Dispatcher : IDisposable
    {
        private bool disposedValue = false;
        private readonly Elements.Core core;
        private Services.ILogger logger;
        private Services.Supervisor supervisor;
        private Services.AdapterMal mal;
        private readonly Services.MalWatcher malWatcher;
        private Services.IRtdb rtdb;
        private Services.MalDirect.CimProfile cim;

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (rtdb != null)
                    {
                        rtdb.Dispose();
                        rtdb = null;
                    }
                    if (mal != null)
                    {
                        mal.Dispose();
                        mal = null;
                    }
                    if (supervisor != null)
                    {
                        supervisor.Dispose();
                        supervisor = null;
                    }
                    if (logger != null)
                    {
                        logger.Dispose();
                        logger = null;
                    }
                }
                disposedValue = true;
            }
        }

        private void CloseAction(string element, Action action)
        {
            try
            {
                action();
                logger.Info($"Dispatcher> {element} closed");
            }
            catch (Exception ex)
            {
                logger.Warning($"Dispatcher> Error close {element}: {ex.Message}");
            }
        }

        private Dispatcher(Services.ILogger logger, Services.Options options)
        {
            this.logger = logger;
            core = new Elements.Core(logger);
            supervisor = new Services.Supervisor(logger);
            mal = new Services.AdapterMal("Aggregator2");
            malWatcher = new Services.MalWatcher(this);
            rtdb = new Services.Rtdbs.Rtdb3(logger);
            core.Rtdb = rtdb;
            Globals.Tau = options.Tau;
            Globals.Quota = options.Quota / 100.0;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Open()
        {
            logger.Info("Dispatcher> Program initializing");
            logger.Info("Dispatcher> Supervisor connecting");
            supervisor.Connect();
            string malConnectionString = supervisor.GetMainModelConnectionString();
            string rtdbConnectionString = supervisor.GetRtdbConnectionString();
            logger.Info($"Dispatcher> MAL connection string: {malConnectionString}");
            logger.Info($"Dispatcher> RTDB connection string: {rtdbConnectionString}");
            supervisor.Disconnect();
            if (string.IsNullOrEmpty(malConnectionString))
                throw new Exception("MAL connection string not defined");
            if (string.IsNullOrEmpty(rtdbConnectionString))
                throw new Exception("RTDB connection string not defined");

            logger.InitComplete();

            logger.Info("Dispatcher> MAL connecting");
            mal.Connect(logger, malConnectionString);
            logger.Info("Dispatcher> RTDB connecting");
            rtdb.Connect(rtdbConnectionString);
            if (!rtdb.IsConnected)
                return;

            cim = new Services.MalDirect.CimProfile(mal.Provider);

            logger.Info("Dispatcher> Building internal model");
            CreateBuilder().Build();
            core.CheckSingleAValue();
            logger.Info($"Dispatcher> Loaded from model: {core.AggregatedAnalogValues.Count} AggregatedAnalogValue(s)");

            if (core.SingleAValue == null && Globals.UseExport)
            {
                logger.Info("Dispatcher> Export internal model to xml");
                Export();
            }

            core.SetMinuteInterval();
            core.CreateRealtimeIntervals();

            logger.Info("Dispatcher> Read initial aggregated analog values");
            core.InitializeValues();

            logger.Info("Dispatcher> Subscribe values");
            core.SubscribeValues();

            logger.Info("Dispatcher> Subscribe events");
            core.SubscribeEvents();

            if (core.SingleAValue == null)
            {
                logger.Info("Dispatcher> Subscribe MAL updates");
                mal.Attach(malWatcher);
            }

            logger.Info("Dispatcher> Program initialization complete");
        }

        private void Close()
        {
            logger.Info("Dispatcher> Closing program");
            CloseAction("RTDB", () => rtdb.Disconnect());
            CloseAction("MAL", () => mal.Disconnect());
            CloseAction("Supervisor", () => supervisor.Disconnect());
        }

        private void Export()
        {
            try
            {
                using var writer = new Services.ExportWriter("Aggregator2Export.xml");
                writer.Write(core.AggregatedAnalogValues);
                writer.Write(core.Calendar);
                writer.Flush();
            }
            catch (Exception ex)
            {
                logger.Warning($"ExportWriter> Error: {ex.Message}");
            }
        }

        private void Execute()
        {
            if (!rtdb.IsConnected)
            {
                Terminated = true;
                logger.Error("Dispatcher> Program aborted: RTDB is disconnected");
                return;
            }

            if (!rtdb.IsValuesSubscriptionActive)
            {
                Terminated = true;
                logger.Error("Dispatcher> Program aborted: RTDB values subscription aborted");
                return;
            }

            if (!rtdb.IsEventsSubscriptionActive)
            {
                Terminated = true;
                logger.Error("Dispatcher> Program aborted: RTDB events subscription aborted");
                return;
            }

            if (core.SingleAValue == null)
            {
                if (malWatcher.IsMalServiceDown)
                {
                    Terminated = true;
                    logger.Error("Dispatcher> Program aborted: MAL service down");
                    return;
                }
                if (malWatcher.IsNeedUpdate())
                    mal.Provider.Update();
            }

            rtdb.ReadSubscriptionValues(core);
            rtdb.ReadSubscriptionEvents(core);

            if (core.IsMinuteIntervalFinished())
            {
                core.ExecuteRealtime();
                core.NextMinuteInterval();
            }
            else if (Globals.UseRecalc)
                core.ExecuteRecalc();

            rtdb.Write();
        }

        public Services.MalDirect.ModelBuilder CreateBuilder()
        {
            return new Services.MalDirect.ModelBuilder(core, logger, cim);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void ConsoleWriteLine(string message)
        {
            Console.WriteLine(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void CheckAnyKey()
        {
            if (Console.KeyAvailable)
                Terminated = true;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void ReadAnyKey()
        {
            Console.ReadKey();
        }

        public static void CreateAndRun(Services.ILogger logger, Services.Options options)
        {
            try
            {
                using var dispatcher = new Dispatcher(logger, options);
                try
                {
                    dispatcher.Open();
                    ConsoleWriteLine("Press any key for exit...");

                    while (!Terminated)
                    {
                        dispatcher.Execute();
                        System.Threading.Thread.Sleep(1);
                        CheckAnyKey();
                    }

                    dispatcher.Close();
                }
                catch (Exception ex)
                {
                    logger.Error($"Dispatcher> Internal error: {ex.Message}");
                    logger.Error(ex);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Dispatcher> Internal error: {ex.Message}");
                logger.Error(ex);
                ConsoleWriteLine(ex.Message);
                ConsoleWriteLine("Press any key for exit...");
                ReadAnyKey();
            }
        }

        public static bool Terminated { get; set; }
    }
}
