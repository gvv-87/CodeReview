using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleDay : ISchedule
    {
        public override string ToString()
        {
            return nameof(ScheduleDay);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            var from = time.AlignToDay();
            return new TimeInterval(from, from.AddDays(1));
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return CreateInterval(avalue, time);
        }
    }
}
