using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aggregator.Elements
{
    public sealed class Core
    {
        private readonly Queue<Guid> recalcQueue;
        private readonly Stopwatch debugTimer;

        private void CheckWorktime(string name)
        {
            if (Logger.NeedWorktimeInfo)
            {
                var time = debugTimer.ElapsedMilliseconds;
                if (time >= Globals.MinLoggingWorktime)
                    Logger.Debug($"Core> {name} for {time} ms");
            }
        }

        public Core(Services.ILogger logger)
        {
            Logger = logger;
            Elements = new AbstractElements();
            AggregatedAnalogValues = new AggregatedAnalogValues();
            MeasurementValues = new MeasurementValues();
            Calendar = new Calendar();
            recalcQueue = new Queue<Guid>();
            debugTimer = new Stopwatch();
            debugTimer.Start();
        }

        public void CheckSingleAValue()
        {
            if (Globals.SingleAValueGuid != null)
                SingleAValue = AggregatedAnalogValues.Find(Globals.SingleAValueGuid.Value);
        }

        public void CreateRealtimeIntervals()
        {
            if (SingleAValue != null)
                SingleAValue.CreateRealtimeInterval(StartMinuteTime);
            else
            {
                foreach (var p in AggregatedAnalogValues)
                    p.CreateRealtimeInterval(StartMinuteTime);
            }
        }

        public void InitializeValues()
        {
            if (!Globals.UseRecalc)
                return;

            debugTimer.Restart();
            if (SingleAValue != null)
                Rtdb.InitializeValues(this, new List<Guid>() { SingleAValue.Uid });
            else
                Rtdb.InitializeValues(this, AggregatedAnalogValues.GetUids());
            CheckWorktime("Initialize values");
        }

        public void SubscribeValues()
        {
            var uids = new List<Guid>();
            if (SingleAValue != null)
            {
                if (SingleAValue.Source != null && SingleAValue.Source.IsFixedStep)
                    uids.Add(SingleAValue.Source.Uid);
            }
            else
            {
                foreach (var mvalue in MeasurementValues)
                {
                    if (mvalue.IsFixedStep)
                        uids.Add(mvalue.Uid);
                }
            }
            Rtdb.SubscribeValues(uids);
        }

        public void SubscribeEvents()
        {
            Rtdb.SubscribeEvents();
        }

        public void OnReadSubscriptionEvent(EventDataItem item)
        {
            if (!Globals.UseRecalc)
                return;

            if (SingleAValue != null)
            {
                if (item.IsSuitable(SingleAValue.Uid) && item.IsSuitable(SingleAValue.AggregationMethodType))
                    SingleAValue.AddRecalcInterval(item.Start, item.End, item.Start);
            }
            else
            {
                foreach (var avalue in AggregatedAnalogValues)
                {
                    if (item.IsSuitable(avalue.Uid) && item.IsSuitable(avalue.AggregationMethodType))
                        avalue.AddRecalcInterval(item.Start, item.End, item.Start);
                }
            }
        }

        public void AddRecalc(Guid uid)
        {
            recalcQueue.Enqueue(uid);
        }

        public void ExecuteRealtime()
        {
            debugTimer.Restart();
            var interval = new TimeInterval(StartMinuteTime, EndMinuteTime);

            if (SingleAValue != null)
                SingleAValue.ExecuteRealtime(interval);
            else
            {
                foreach (var p in AggregatedAnalogValues)
                    p.ExecuteRealtime(interval);
            }

            CheckWorktime("Realtime calculation");
        }

        public void ExecuteRecalc()
        {
            debugTimer.Restart();
            const int maxRecalcTime = 2000; // ms
            int endTime = Environment.TickCount + maxRecalcTime;

            while (recalcQueue.Count != 0 && Environment.TickCount < endTime)
            {
                var uid = recalcQueue.Dequeue();
                var avalue = AggregatedAnalogValues.Find(uid);
                if (avalue != null)
                {
                    if (SingleAValue == null || ReferenceEquals(SingleAValue, avalue))
                    {
                        avalue.ExecuteRecalc();
                        if (avalue.HasRecalcIntervals)
                            recalcQueue.Enqueue(avalue.Uid);
                    }
                }
            }

            CheckWorktime("Recalc");
        }

        public void SetMinuteInterval()
        {
            SetMinuteInterval(Globals.CurrentTime);
        }

        public void SetMinuteInterval(DateTime time)
        {
            StartMinuteTime = time.AlignToMinute();
            EndMinuteTime = StartMinuteTime.AddMinutes(1);
        }

        public void NextMinuteInterval()
        {
            StartMinuteTime = EndMinuteTime;
            EndMinuteTime = StartMinuteTime.AddMinutes(1);
        }

        public bool IsMinuteIntervalFinished()
        {
            return Globals.CurrentTime >= EndMinuteTime;
        }

        public Services.ILogger Logger { get; private set; }
        public Services.IRtdb Rtdb { get; set; }
        public AbstractElements Elements { get; private set; }
        public AggregatedAnalogValues AggregatedAnalogValues { get; private set; }
        public MeasurementValues MeasurementValues { get; private set; }
        public Calendar Calendar { get; private set; }

        // Временная точка, соответствущая началу текущего минутного интервала, выровненного точно по минуте
        public DateTime StartMinuteTime { get; private set; }
        // Временная точка, соответствующая концу текущего минутного интервала и началу нового минутного интервала
        public DateTime EndMinuteTime { get; private set; }
        // Единственный элемент для обработки в тестовом режиме (или null в нормальном режиме)
        public AggregatedAnalogValue SingleAValue { get; private set; }
    }
}
