using System;

namespace Aggregator.Calc
{
    public class AbstractCalcData
    {
        private int intermediateStep;
        private DateTime nextWriteTime;
        private ValueDataItem prevItem;

        protected void Initialize(ICalcData owner, Elements.AggregatedAnalogValue avalue, RecalcInterval wholeInterval,
            int intermediateStep, TimeInterval partialInterval = null)
        {
            WholeInterval = wholeInterval;
            this.intermediateStep = intermediateStep;
            nextWriteTime = wholeInterval.From.AddSeconds(intermediateStep);
            if (avalue.ScheduleType == Monitel.Mal.Context.CIM16.AggregationSchedule.interval)
            {
                if (nextWriteTime > WholeInterval.To)
                    nextWriteTime = WholeInterval.To;
            }

            var interval = new TimeInterval(wholeInterval.From, nextWriteTime);
            DateTime endTo = (partialInterval != null)
                ? partialInterval.From // вычисление на начальной части полного интервала вплоть то partialInterval
                : wholeInterval.To;    // вычисление на всём полном интервале

            while (interval.To <= endTo)
            {
                ExecutePastMinute(owner, avalue, interval);
                interval = new TimeInterval(interval.To, interval.To.AddSeconds(intermediateStep));
            }
        }

        protected void ExecutePastMinute(ICalcData owner, Elements.AggregatedAnalogValue avalue, TimeInterval interval)
        {
            if (WholeInterval == null)
                return;

            if (interval.To >= nextWriteTime)
            {
                avalue.AggregationMethod.Execute(avalue, owner, interval);
                if (interval.From >= WholeInterval.Start)
                    avalue.AggregationMethod.Write(avalue, owner, WholeInterval.From, interval.To);
                nextWriteTime = nextWriteTime.AddSeconds(intermediateStep);
            }
        }

        public bool CanWrite(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
            bool result = Math.Abs(item.Value - prevItem.Value) >= 1e-6
                || item.QCode != prevItem.QCode
                || avalue.StepIntervalInSeconds != 0;
            prevItem = item;
            return result;
        }

        public RecalcInterval WholeInterval { get; private set; }
    }
}
