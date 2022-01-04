using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleEnergyWeek : ISchedule
    {
        private int FindDayIndex(Calendar calendar, DateTime time)
        {
            return calendar.FindIndex(time);
        }

        private TimeInterval CreateInterval(Calendar calendar, int startIndex, int endIndex)
        {
            if (startIndex >= 0 && endIndex >= 0)
            {
                var startDay = calendar.DayByIndex(startIndex);
                var endDay = calendar.DayByIndex(endIndex);
                if (startDay != null && endDay != null)
                {
                    var from = startDay.Date;
                    var to = endDay.Date;
                    if (from != null && to != null)
                        return new TimeInterval(from, to);
                }
            }
            return null;
        }

        public override string ToString()
        {
            return nameof(ScheduleEnergyWeek);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            var calendar = avalue.Core.Calendar;
            var index = FindDayIndex(calendar, time);
            if (index < 0)
                return null;

            var day = calendar.DayByIndex(index);
            if (day == null)
                return null;
            if (!avalue.Contains(day.DayType))
                return null;

            var startIndex = calendar.FindStartIndexBack(avalue, index);
            var endIndex = calendar.FindEndIndex(avalue, index + 1);
            return CreateInterval(calendar, startIndex, endIndex);
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            var calendar = avalue.Core.Calendar;
            var index = FindDayIndex(calendar, time);
            if (index < 0)
                return null;

            var day = calendar.DayByIndex(index);
            if (day == null)
                return null;
            int startIndex = avalue.Contains(day.DayType)
                ? calendar.FindStartIndexBack(avalue, index)
                : calendar.FindStartIndexForward(avalue, index);
            int endIndex = calendar.FindEndIndex(avalue, startIndex + 1);
            return CreateInterval(calendar, startIndex, endIndex);
        }
    }
}
