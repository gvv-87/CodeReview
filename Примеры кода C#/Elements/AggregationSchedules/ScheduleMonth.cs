using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleMonth : ISchedule
    {
        public override string ToString()
        {
            return nameof(ScheduleMonth);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            var from = new DateTime(time.Year, time.Month, 1, 0, 0, 0, DateTimeKind.Local);
            var to = from.AddMonths(1);
            return new TimeInterval(from, to);
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return CreateInterval(avalue, time);
        }
    }
}
