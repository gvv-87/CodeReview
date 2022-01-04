using System;

namespace Aggregator.Calc
{
    public sealed class RealtimeManager
    {
        private TimeInterval nextInterval;
        private int intermediateStep;
        private bool initialized;
        private DateTime start;
        private readonly ICalcData methodData;

        private bool IsCurrentIntervalFinished(TimeInterval interval)
        {
            return CurrentInterval != null && interval.From >= CurrentInterval.To;
        }

        public RealtimeManager(Elements.AggregatedAnalogValue avalue, TimeInterval currentInterval,
            TimeInterval nextInterval, int intermediateStep)
        {
            CurrentInterval = currentInterval;
            this.nextInterval = nextInterval;
            this.intermediateStep = intermediateStep;
            initialized = false;
            methodData = avalue.AggregationMethod.CreateMethodData();
        }

        public void Execute(Elements.AggregatedAnalogValue avalue, TimeInterval interval)
        {
            if (interval == null)
                return;

            if (IsCurrentIntervalFinished(interval))
                CurrentInterval = null;

            if (CurrentInterval == null)
            {
                initialized = false;

                // На тот случай, если модель будет изменена
                intermediateStep = avalue.GetIntermediateStep();

                if (nextInterval == null)
                {
                    CurrentInterval = avalue.Schedule.CreateInterval(avalue, interval.From);
                    if (CurrentInterval == null)
                        nextInterval = avalue.Schedule.CreateNextInterval(avalue, interval.From);
                }
                else if (nextInterval.Contains(interval.From))
                {
                    CurrentInterval = nextInterval;
                    nextInterval = null;
                }

                if (CurrentInterval == null)
                    return;
            }

            if (interval.To <= CurrentInterval.From)
                return;

            if (!initialized)
            {
                initialized = true;
                if (avalue.LastStartTime != null)
                {
                    start = avalue.LastStartTime.Value;
                    avalue.LastStartTime = null;
                    if (start < CurrentInterval.From)
                        avalue.AddRecalcInterval(start, CurrentInterval.From, start);
                    else if (start >= CurrentInterval.To)
                        start = CurrentInterval.From;
                }
                else
                    start = CurrentInterval.From;
                var rinterval = new RecalcInterval(CurrentInterval.From, CurrentInterval.To, start);
                methodData.Initialize(avalue, rinterval, intermediateStep, interval);
            }

            methodData.ExecutePastMinute(avalue, interval);
        }

        public void OnValueSubscriptionReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem d)
        {
            if (CurrentInterval != null)
                methodData.OnValueSubscriptionReceived(avalue, d);
        }

        public TimeInterval CurrentInterval { get; private set; }
    }
}
