namespace Aggregator.Elements.AggregationValueFilterTypes
{
    public sealed class ValueFilterPositive : IValueFilter
    {
        public bool IsValid(double value)
        {
            return value > 0;
        }

        public bool CanApplyByChange()
        {
            return true;
        }

        public FilterTypeByChange GetFilterTypeByChange()
        {
            return FilterTypeByChange.positive;
        }

        public override string ToString()
        {
            return nameof(ValueFilterPositive);
        }
    }
}
