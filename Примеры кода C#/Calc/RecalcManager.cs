using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Aggregator.Calc
{
    public sealed class RecalcManager
    {
        private readonly LinkedList<RecalcInterval> list;
        private readonly LinkedList<RecalcInterval> additionalIntervals;
        private readonly Stopwatch debugTimer;

        // Формирование нормализованных начала и конца интервала:
        // - если конца нет, то он определяется равным началу
        // - если конец меньше начала, то они меняются местами
        private void Normalize(DateTime from, DateTime? to, out DateTime normalFrom, out DateTime normalTo)
        {
            if (to == null)
            {
                normalFrom = from;
                normalTo = from;
            }
            else if (from < to.Value)
            {
                normalFrom = from;
                normalTo = to.Value;
            }
            else
            {
                normalFrom = to.Value;
                normalTo = from;
            }
        }

        // Определение интервала по заданной точке
        private TimeInterval CreateInterval(Elements.AggregatedAnalogValue avalue, DateTime p)
        {
            var interval = avalue.Schedule.CreateInterval(avalue, p);
            if (interval == null)
                interval = avalue.Schedule.CreateNextInterval(avalue, p);
            if (interval == null)
                avalue.Core.Logger.Warning($"RecalcManager> AggregatedAnalogValue {avalue.Uid}. Can't create recalc interval for {p}");
            return interval;
        }

        // Корректирование входного интервала (start..end) по текущему интервалу реального времени realtimeInterval:
        // - если входной интервал слева от realtimeInterval, то он не меняется
        // - если входной интервал справа от realtimeInterval, то он игнорируется
        // - если входной интервал полностью внутри realtimeInterval, то он игнорируется
        // - если входной интервал слева и частично заходит в realtimeInterval, то он ограничивается началом realtimeInterval
        // - если входной интервал справа и частично заходит в realtimeInterval, то он игнорируется
        // - если входной интервал охватывает realtimeInterval, то он ограничивается началом realtimeInterval
        private void Correct(TimeInterval realtimeInterval, ref DateTime from, ref DateTime to)
        {
            if (realtimeInterval == null)
                return;

            if (from < realtimeInterval.From)       // начало слева
            {
                if (to <= realtimeInterval.From)    // конец слева
                {
                }
                else
                {
                    to = realtimeInterval.From;
                }
                //else if (to <= realtimeInterval.To) // конец внутри
                //{
                //    to = realtimeInterval.From;
                //}
                //else                                // конец справа
                //{
                //    to = realtimeInterval.From;
                //}
            }
            //else if (from < realtimeInterval.To)    // начало внутри
            //{
            //    if (to <= realtimeInterval.To)      // конец внутри
            //    {
            //        to = from;
            //    }
            //    else                                // конец справа
            //    {
            //        to = from;
            //    }
            //}
            else                                    // начало справа
            {
                to = from;
            }
        }

        // Поиск и расширение существующего интервала или вставка нового интервала
        private void Insert(DateTime from, DateTime to, DateTime start)
        {
            if (from == to) // игнорируем пустой интервал
                return;

            for (var node = list.First; node != null; node = node.Next)
            {
                var i = node.Value;
                if (i.Contains(from))
                {
                    if (i.Contains(to))
                    {
                        // Добавляемый интервал содержится внутри существующего, игнорируем его
                    }
                    else
                    {
                        // Расширяем существующий интервал справа и удаляем его, добавляя расширенный интервал на дообработку
                        DateTime newStart = (start < i.Start) ? start : i.Start;
                        additionalIntervals.AddLast(new RecalcInterval(i.From, to, newStart));
                        list.Remove(node);
                    }
                    return;
                }

                if (i.Contains(to))
                {
                    // Расширяем существующий интервал слева и удаляем его, добавляя расширенный интервал на дообработку
                    DateTime newStart = (start < i.Start) ? start : i.Start;
                    additionalIntervals.AddLast(new RecalcInterval(from, i.To, newStart));
                    list.Remove(node);
                    return;
                }

                if (from < i.From && to >= i.To)
                {
                    // Расширяем существующий интервал слева и справа и удаляем его, добавляя расширенный интервал на дообработку
                    DateTime newStart = (start < i.Start) ? start : i.Start;
                    additionalIntervals.AddLast(new RecalcInterval(from, to, newStart));
                    list.Remove(node);
                    return;
                }
            }

            // Расширяемых интервалов не обнаружено, добавляем новый
            list.AddLast(new RecalcInterval(from, to, start));
        }

        // Добавление входного интервала from..to, считая, что он уже нормализован
        private void Add(Elements.AggregatedAnalogValue avalue, DateTime from, DateTime to, DateTime start,
            TimeInterval realtimeInterval)
        {
            Correct(realtimeInterval, ref from, ref to);
            Insert(from, to, start);

            var node = additionalIntervals.First;
            while (node != null)
            {
                additionalIntervals.RemoveFirst();
                Add(avalue, node.Value.From, node.Value.To, node.Value.Start, realtimeInterval);
                node = additionalIntervals.First;
            }
        }

        private void ExecuteInternal(Elements.AggregatedAnalogValue avalue, RecalcInterval wholeInterval)
        {
            var intermediateStep = avalue.GetIntermediateStep();
            var data = avalue.AggregationMethod.CreateMethodData();
            var startPoint = wholeInterval.From;
            var logger = avalue.Core.Logger;

            if (logger.NeedRecalcInfo)
                logger.Debug($"Recalc> Execute {avalue.Uid}: {wholeInterval.ToString()}");

            debugTimer.Restart();
            while (true)
            {
                var interval = avalue.Schedule.CreateInterval(avalue, startPoint);
                if (interval == null)
                    interval = avalue.Schedule.CreateNextInterval(avalue, startPoint);
                if (interval == null)
                    break;
                if (wholeInterval.From >= interval.To)
                    break;
                data.Initialize(avalue, new RecalcInterval(interval.From, interval.To, wholeInterval.Start), intermediateStep);
                startPoint = interval.To;
            }

            if (logger.NeedWorktimeInfo)
            {
                var time = debugTimer.ElapsedMilliseconds;
                if (time >= Globals.MinLoggingWorktime)
                    logger.Debug($"Recalc> Done {avalue.Uid}: {wholeInterval.ToString()} for {time} ms");
            }
        }

        public RecalcManager()
        {
            list = new LinkedList<RecalcInterval>();
            additionalIntervals = new LinkedList<RecalcInterval>();
            debugTimer = new Stopwatch();
            debugTimer.Start();
        }

        public void Add(Elements.AggregatedAnalogValue avalue, DateTime from, DateTime? to, DateTime? start, TimeInterval realtimeInterval)
        {
            if (!Globals.UseRecalc)
                return;

            Normalize(from, to, out DateTime normalFrom, out DateTime normalTo);

            var interval = CreateInterval(avalue, normalFrom);
            if (interval == null)
                return;

            var fromPoint = interval.From;
            var toPoint = interval.To;

            if (normalTo > toPoint)
                toPoint = normalTo;

            var startPoint = (start == null) ? fromPoint : start.Value;

            Add(avalue, fromPoint, toPoint, startPoint, realtimeInterval);
            if (list.Count != 0)
                avalue.Core.AddRecalc(avalue.Uid);
        }

        public void Execute(Elements.AggregatedAnalogValue avalue, TimeInterval realtimeInterval)
        {
            var node = list.First;
            if (node != null)
            {
                var interval = node.Value;
                list.RemoveFirst();
                if (interval == null)
                    return;
                if (realtimeInterval != null)
                {
                    if (interval.From >= realtimeInterval.From)
                        return;
                    if (interval.To > realtimeInterval.From)
                        interval = new RecalcInterval(interval.From, realtimeInterval.From, interval.Start);
                }
                ExecuteInternal(avalue, interval);
            }
        }

        public bool HasDeferredInterval(DateTime from, DateTime to, DateTime start)
        {
            foreach (var i in list)
            {
                if (i.Equals(from, to, start))
                    return true;
            }
            return false;
        }

        public int GetDeferredCount()
        {
            return list.Count;
        }

        public bool IsActive
        {
            get { return GetDeferredCount() != 0; }
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            var first = true;
            foreach (var i in list)
            {
                if (first)
                    first = false;
                else
                    sb.Append(", ");
                sb.Append('{').Append(i.ToString()).Append('}');
            }
            return sb.ToString();
        }
    }
}
