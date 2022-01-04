using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleYear : ISchedule
    {
        public override string ToString()
        {
            return nameof(ScheduleYear);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            var from = new DateTime(time.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
            var to = new DateTime(time.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Local);
            return new TimeInterval(from, to);
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return CreateInterval(avalue, time);
        }
    }
}
