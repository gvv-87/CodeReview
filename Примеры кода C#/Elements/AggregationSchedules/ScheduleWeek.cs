using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleWeek : ISchedule
    {
        public override string ToString()
        {
            return nameof(ScheduleWeek);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            var calendar = avalue.Core.Calendar;
            var from = calendar.BeginWeek(time);
            var to = calendar.BeginNextWeek(time);
            if (from != null && to != null)
                return new TimeInterval(from.Value, to.Value);
            return null;
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return CreateInterval(avalue, time);
        }
    }
}
