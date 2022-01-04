using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleInterval : ISchedule
    {
        private readonly DayTimeRange dayTimeRange;

        public ScheduleInterval(DateTime start, DateTime end)
        {
            dayTimeRange = new DayTimeRange(start, end);
        }

        public override string ToString()
        {
            return nameof(ScheduleInterval);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return dayTimeRange.CreateInterval(time);
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return dayTimeRange.CreateNextInterval(time);
        }
    }
}
