using System;
using System.Collections.Generic;

namespace Aggregator.Calc
{
    public sealed class CalcDataFixedStep : AbstractCalcData, ICalcData
    {
        private readonly SortedList<DateTime, ValueDataItem> items;

        public CalcDataFixedStep()
        {
            items = new SortedList<DateTime, ValueDataItem>();
        }

        public void Initialize(Elements.AggregatedAnalogValue avalue, RecalcInterval wholeInterval,
            int intermediateStep, TimeInterval partialInterval = null)
        {
            items.Clear();
            Initialize(this, avalue, wholeInterval, intermediateStep, partialInterval);
            avalue.Core.Rtdb.ReadIntervalValues(avalue, this, new TimeInterval(wholeInterval.From, wholeInterval.To));
        }

        public void ExecutePastMinute(Elements.AggregatedAnalogValue avalue, TimeInterval interval)
        {
            ExecutePastMinute(this, avalue, interval);
        }

        public void OnValueIntervalReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
            if (WholeInterval == null)
                return;

            DateTime t;
            if (item.Time < WholeInterval.From)
                t = WholeInterval.From;
            else if (item.Time > WholeInterval.To)
                t = WholeInterval.To;
            else
                t = item.Time;

            if (items.Count == 0)
                items[t] = item;
            else
            {
                var values = items.Values;
                if (item.QCode == 0 && values[values.Count - 1].QCode != 0)
                    items[t] = item;
                else if (item.QCode != 0)
                    items[t] = item;
            }
        }

        public void OnValueSubscriptionReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
            if (WholeInterval == null)
                return;

            var t = item.Time;
            if (t >= WholeInterval.From && t < WholeInterval.To)
            {
                if (item.QCode == 0 && items.ContainsKey(item.Time))
                    return;
                items[t] = item;
            }
        }

        public void OnValueStatisticalReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
        }

        public IList<ValueDataItem> Items
        {
            get { return items.Values; }
        }
    }
}
