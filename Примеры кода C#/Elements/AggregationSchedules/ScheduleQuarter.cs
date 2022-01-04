using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleQuarter : ISchedule
    {
        public override string ToString()
        {
            return nameof(ScheduleQuarter);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            int month;
            switch (time.Month)
            {
                case 1:
                case 2:
                case 3:
                    month = 1;
                    break;
                case 4:
                case 5:
                case 6:
                    month = 4;
                    break;
                case 7:
                case 8:
                case 9:
                    month = 7;
                    break;
                default:
                    month = 10;
                    break;
            }
            var from = new DateTime(time.Year, month, 1, 0, 0, 0, DateTimeKind.Local);
            var to = from.AddMonths(3);
            return new TimeInterval(from, to);
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return CreateInterval(avalue, time);
        }
    }
}
