using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleTimeZone : ISchedule
    {
        public override string ToString()
        {
            return nameof(ScheduleTimeZone);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            if (avalue == null)
                return null;
            if (avalue.TimeZone == null)
                return null;

            var calendar = avalue.Core.Calendar;
            var index = calendar.FindIndex(time);
            if (index < 0)
                return null;

            var day = calendar.DayByIndex(index);
            if (day == null)
                return null;
            if (!avalue.Contains(day.DayType))
                return null;

            return avalue.TimeZone.CreateInterval(time);
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            if (avalue == null)
                return null;
            if (avalue.TimeZone == null)
                return null;

            var calendar = avalue.Core.Calendar;
            var index = calendar.FindIndex(time);
            if (index < 0)
                return null;

            var day = calendar.DayByIndex(index);
            if (day == null)
                return null;
            if (avalue.Contains(day.DayType))
                return avalue.TimeZone.CreateNextInterval(time);

            index = calendar.FindStartIndexForward(avalue, index);
            if (index < 0)
                return null;
            day = calendar.DayByIndex(index);
            if (day == null)
                return null;
            return avalue.TimeZone.CreateNextInterval(day.Date);
        }
    }
}
