namespace Aggregator.Elements.AggregationValueFilterTypes
{
    public sealed class ValueFilterGreaterOrEqual : IValueFilter
    {
        private double filterValue;

        public ValueFilterGreaterOrEqual(double filterValue)
        {
            this.filterValue = filterValue;
        }

        public bool IsValid(double value)
        {
            return value >= filterValue;
        }

        public bool CanApplyByChange()
        {
            return false;
        }

        public FilterTypeByChange GetFilterTypeByChange()
        {
            return FilterTypeByChange.all;
        }

        public override string ToString()
        {
            return $"{nameof(ValueFilterGreaterOrEqual)}, {filterValue}";
        }
    }
}
