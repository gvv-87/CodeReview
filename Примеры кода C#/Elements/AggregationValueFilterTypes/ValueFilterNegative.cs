namespace Aggregator.Elements.AggregationValueFilterTypes
{
    public sealed class ValueFilterNegative : IValueFilter
    {
        public bool IsValid(double value)
        {
            return value < 0;
        }

        public bool CanApplyByChange()
        {
            return true;
        }

        public FilterTypeByChange GetFilterTypeByChange()
        {
            return FilterTypeByChange.negative;
        }

        public override string ToString()
        {
            return nameof(ValueFilterNegative);
        }
    }
}
