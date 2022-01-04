namespace Aggregator.Elements.AggregationValueFilterTypes
{
    public sealed class ValueFilterAll : IValueFilter
    {
        public bool IsValid(double value)
        {
            return true;
        }

        public bool CanApplyByChange()
        {
            return true;
        }

        public FilterTypeByChange GetFilterTypeByChange()
        {
            return FilterTypeByChange.all;
        }

        public override string ToString()
        {
            return nameof(ValueFilterAll);
        }
    }
}
