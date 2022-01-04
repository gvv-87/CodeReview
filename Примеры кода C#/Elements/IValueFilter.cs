namespace Aggregator.Elements
{
    public enum FilterTypeByChange : byte
    {
        all = 0,
        negative = 1,
        positive = 2
    }

    public interface IValueFilter
    {
        bool IsValid(double value);
        bool CanApplyByChange();
        FilterTypeByChange GetFilterTypeByChange();
    }
}
