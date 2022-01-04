using System;

namespace Aggregator.Elements
{
    public sealed class AggregatedAnalogValueData
    {
        private Calc.RealtimeManager realtimeManager;

        public void CreateRealtimeInterval(AggregatedAnalogValue avalue, TimeInterval currentInterval,
            TimeInterval nextInterval, int intermediateStep)
        {
            realtimeManager = new Calc.RealtimeManager(avalue, currentInterval, nextInterval, intermediateStep);
        }

        public void AddRecalcInterval(AggregatedAnalogValue avalue, DateTime from, DateTime? to, DateTime? start)
        {
            if (RecalcManager == null)
                RecalcManager = new Calc.RecalcManager();
            RecalcManager.Add(avalue, from, to, start, realtimeManager?.CurrentInterval);
        }

        public void ExecuteRealtime(AggregatedAnalogValue avalue, TimeInterval interval)
        {
            if (realtimeManager != null)
                realtimeManager.Execute(avalue, interval);
        }

        public void ExecuteRecalc(AggregatedAnalogValue avalue)
        {
            if (RecalcManager != null)
            {
                RecalcManager.Execute(avalue, realtimeManager?.CurrentInterval);
                if (!RecalcManager.IsActive)
                    RecalcManager = null;
            }
        }

        public void OnInitialize(AggregatedAnalogValue avalue, DateTime lastTime)
        {
            AddRecalcInterval(avalue, lastTime, null, lastTime);
        }

        public void OnValueSubscriptionReceived(AggregatedAnalogValue avalue, ValueDataItem item)
        {
            if (realtimeManager != null)
                realtimeManager.OnValueSubscriptionReceived(avalue, item);
        }

        public bool HasRecalcIntervals => (RecalcManager != null) ? RecalcManager.IsActive : false;

        public Calc.RecalcManager RecalcManager { get; private set; }
        public DateTime? LastStartTime { get; set; }
    }
}
