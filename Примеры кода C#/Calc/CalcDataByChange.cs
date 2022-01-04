using System.Collections.Generic;

namespace Aggregator.Calc
{
    public sealed class CalcDataByChange : AbstractCalcData, ICalcData
    {
        private readonly List<ValueDataItem> items;

        public CalcDataByChange()
        {
            items = new List<ValueDataItem>();
        }

        public void Initialize(Elements.AggregatedAnalogValue avalue, RecalcInterval wholeInterval,
            int intermediateStep, TimeInterval partialInterval = null)
        {
            items.Clear();
            Initialize(this, avalue, wholeInterval, intermediateStep, partialInterval);
        }

        public void ExecutePastMinute(Elements.AggregatedAnalogValue avalue, TimeInterval interval)
        {
            ExecutePastMinute(this, avalue, interval);
        }

        public void OnValueIntervalReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
        }

        public void OnValueSubscriptionReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
        }

        public void OnValueStatisticalReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item)
        {
            items.Add(item);
        }

        public IList<ValueDataItem> Items
        {
            get { return items; }
        }
    }
}
