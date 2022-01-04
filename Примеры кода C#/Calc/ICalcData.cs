using System.Collections.Generic;

namespace Aggregator.Calc
{
    public interface ICalcData
    {
        void Initialize(Elements.AggregatedAnalogValue avalue, RecalcInterval wholeInterval,
            int intermediateStep, TimeInterval partialInterval = null);
        void ExecutePastMinute(Elements.AggregatedAnalogValue avalue, TimeInterval interval);
        void OnValueIntervalReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item);
        void OnValueSubscriptionReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item);
        void OnValueStatisticalReceived(Elements.AggregatedAnalogValue avalue, ValueDataItem item);
        bool CanWrite(Elements.AggregatedAnalogValue avalue, ValueDataItem item);
        IList<ValueDataItem> Items { get; }
        RecalcInterval WholeInterval { get; }
    }
}
