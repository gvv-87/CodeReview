using System;

namespace Aggregator.Elements.AggregationSchedules
{
    public sealed class ScheduleRegular : ISchedule
    {
        private readonly int period;

        public ScheduleRegular(int period)
        {
            this.period = period;
        }

        public override string ToString()
        {
            return nameof(ScheduleRegular);
        }

        public TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            // время, выровненное до минуты
            DateTime alignedMinuteTime = time.AlignToMinute();

            // число секунд, прошедших от начала дня
            int secondsOfDay = (int)alignedMinuteTime.TimeOfDay.TotalSeconds;
            // выровненное по периоду число секунд, прошедших от начала дня
            int alignedSecondsOfDay = (secondsOfDay / period) * period;
            int deltaSeconds = secondsOfDay - alignedSecondsOfDay;

            var from = alignedMinuteTime.AddSeconds(-deltaSeconds);
            return new TimeInterval(from, from.AddSeconds(period));
        }

        public TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time)
        {
            return CreateInterval(avalue, time);
        }
    }
}
