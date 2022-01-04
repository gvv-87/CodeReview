namespace Aggregator.Elements
{
    public interface IVisitor
    {
        void Accept(AggregatedAnalogValue obj);
        void Accept(CalendarDay obj);
        void Accept(DayType obj);
        void Accept(HISPartition obj);
        void Accept(MeasurementValue obj);
        void Accept(Season obj);
        void Accept(TimeZone obj);
        void Accept(TimeZoneInSeason obj);
    }
}
