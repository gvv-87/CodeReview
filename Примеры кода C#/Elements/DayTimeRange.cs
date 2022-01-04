using System;

namespace Aggregator.Elements
{
    /// <summary>
    /// Диапазон времен внутри суток. Диапазон может полностью находиться внутри суток или
    /// переходить с одних суток на другие. Например, интервал ночного времени 22:00 .. 06:00
    /// </summary>
    public sealed class DayTimeRange
    {
        // Диапазон времен в секундах от начала суток
        private readonly int startShift;
        private readonly int endShift;
        private DateTime lastFrom;
        private DateTime lastTo;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="start">Начало интервала в локальном времени</param>
        /// <param name="end">Конец интервала в локальном времени</param>
        public DayTimeRange(DateTime start, DateTime end)
        {
            startShift = (int)start.TimeOfDay.TotalSeconds;
            endShift = (int)end.TimeOfDay.TotalSeconds;
        }

        /// <summary>
        /// Создание временного интервала по заданному времени внутри интервала
        /// </summary>
        /// <param name="time">Локальное время</param>
        /// <returns>Созданный объект TimeInterval, если заданное время попадает в хранимый диапазон времен
        /// или null, если не попадает</returns>
        public TimeInterval CreateInterval(DateTime time)
        {
            var aligned = time.AlignToDay();
            var shift = time.TimeOfDay.TotalSeconds;
            if (endShift <= startShift)
            {
                if (shift < endShift)
                {
                    lastFrom = aligned.AddDays(-1).AddSeconds(startShift);
                    lastTo = aligned.AddSeconds(endShift);
                }
                else
                {
                    lastFrom = aligned.AddSeconds(startShift);
                    lastTo = aligned.AddDays(1).AddSeconds(endShift);
                }
            }
            else
            {
                lastFrom = aligned.AddSeconds(startShift);
                lastTo = aligned.AddSeconds(endShift);
            }
            return (time >= lastFrom && time < lastTo) ? new TimeInterval(lastFrom, lastTo) : null;
        }

        /// <summary>
        /// Создание временного интервала, расположенного позже за заданным временем
        /// </summary>
        /// <param name="time">Локальное время</param>
        /// <returns>Созданный объект TimeInterval</returns>
        public TimeInterval CreateNextInterval(DateTime time)
        {
            var interval = CreateInterval(time);
            if (interval != null)
                return interval;
            if (time <= lastFrom)
                return new TimeInterval(lastFrom, lastTo);
            return new TimeInterval(lastFrom.AddDays(1), lastTo.AddDays(1));
        }
    }
}
