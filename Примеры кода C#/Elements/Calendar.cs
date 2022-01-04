using System;
using System.Collections;
using System.Collections.Generic;

namespace Aggregator.Elements
{
    public sealed class Calendar : IEnumerable<CalendarDay>
    {
        private readonly SortedList<int, CalendarDay> days;

        private static int GetKey(DateTime time)
        {
            return time.Year * 100 * 100 + time.Month * 100 + time.Day;
        }

        public Calendar()
        {
            days = new SortedList<int, CalendarDay>();
        }

        public void Add(CalendarDay day)
        {
            if (day == null)
                return;
            days[GetKey(day.Date)] = day;
        }

        public void Remove(CalendarDay day)
        {
            if (day == null)
                return;
            days.Remove(GetKey(day.Date));
        }

        public int FindIndex(DateTime date)
        {
            return days.IndexOfKey(GetKey(date));
        }

        public CalendarDay DayByIndex(int index)
        {
            if (index >= 0 && index < days.Count)
                return days.Values[index];
            return null;
        }

        /// <summary>
        /// Поиск дня, удовлетворяющего типам AggregatedAnalogValue, назад от заданного
        /// </summary>
        /// <param name="index">Индекс дня (удовлетворяющего типам AggregatedAnalogValue)</param>
        /// <returns>Индекс первого подходящего дня или -1</returns>
        public int FindStartIndexBack(AggregatedAnalogValue avalue, int index)
        {
            if (avalue == null)
                return -1;
            if (index < 0 || index >= days.Count)
                return -1;

            int startIndex = index;
            for (int i = index; i >= 0; --i)
            {
                var day = days.Values[i];
                if (day != null)
                {
                    if (!avalue.Contains(day.DayType))
                        return startIndex;
                    startIndex = i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Поиск дня, удовлетворяющего типам AggregatedAnalogValue, вперед от заданного
        /// </summary>
        /// <param name="index">Индекс дня</param>
        /// <returns>Индекс первого подходящего дня или -1</returns>
        public int FindStartIndexForward(AggregatedAnalogValue avalue, int index)
        {
            if (avalue == null)
                return -1;
            if (index < 0 || index >= days.Count)
                return -1;

            for (int i = index; i < days.Count; ++i)
            {
                var day = days.Values[i];
                if (day != null && avalue.Contains(day.DayType))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Поиск дня, не удовлетворяющего типам AggregatedAnalogValue, вперед от заданного
        /// </summary>
        /// <param name="index">Индекс дня</param>
        /// <returns>Индекс первого подходящего дня или -1</returns>
        public int FindEndIndex(AggregatedAnalogValue avalue, int index)
        {
            if (avalue == null)
                return -1;
            if (index < 0 || index >= days.Count)
                return -1;

            for (int i = index; i < days.Count; ++i)
            {
                var day = days.Values[i];
                if (day != null && !avalue.Contains(day.DayType))
                    return i;
            }
            return -1;
        }

        public IEnumerator<CalendarDay> GetEnumerator()
        {
            return days.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return days.Values.GetEnumerator();
        }

        public int Count => days.Count;

        public DateTime? BeginWeek(DateTime date)
        {
            var idx = days.IndexOfKey(GetKey(date));
            if (idx < 0)
                return null;

            var curDay = days.Values[idx];

            while (--idx > 0)
            {
                var prevDay = days.Values[idx];

                if (prevDay.Week != curDay.Week)
                    break;
                if (prevDay.Date.Month != curDay.Date.Month)
                    break;
                if (prevDay.Date.Year != curDay.Date.Year)
                    break;
            }

            return curDay.Date;
        }

        public DateTime? BeginNextWeek(DateTime date)
        {
            var idx = days.IndexOfKey(GetKey(date));
            if (idx < 0)
                return null;

            var curDay = days.Values[idx];
            var nextDay = curDay;

            while (++idx < days.Count)
            {
                nextDay = days.Values[idx];

                if (nextDay.Week != curDay.Week)
                    break;
                if (nextDay.Date.Month != curDay.Date.Month)
                    break;
                if (nextDay.Date.Year != curDay.Date.Year)
                    break;
            }

            return nextDay.Date;
        }
    }
}
