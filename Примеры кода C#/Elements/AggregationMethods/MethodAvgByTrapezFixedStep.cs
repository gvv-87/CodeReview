using System;
using System.Collections.Generic;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodAvgByTrapezFixedStep : IMethod
    {
        private readonly List<Tuple<double, double>> aux = new List<Tuple<double, double>>();

        public MethodTypeByChange GetMethodTypeByChange()
        {
            return MethodTypeByChange.none;
        }

        public Calc.ICalcData CreateMethodData()
        {
            return new Calc.CalcDataFixedStep();
        }

        public void Execute(AggregatedAnalogValue avalue, Calc.ICalcData data, TimeInterval interval)
        {
        }

        public void Write(AggregatedAnalogValue avalue, Calc.ICalcData data, DateTime from, DateTime to)
        {
            var items = data.Items;
            double wvalue = 0;
            double goodIntervalSum = 0;
            DateTime? lastTime = null;
            aux.Clear();

            double totalInterval = to.Subtract(from).TotalSeconds;
            if (totalInterval < 0.1)
                return;

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                if (item.Time >= to)
                    break;
                if (avalue.IsProcessed(item.QCode, item.Value))
                {
                    DateTime tnext = (i == items.Count - 1) ? to : items[i + 1].Time;
                    var interval = tnext.Subtract(item.Time).TotalSeconds;
                    var k = interval / totalInterval;
                    wvalue += item.Value * k;
                    lastTime = item.Time;
                    aux.Add(new Tuple<double, double>(item.Value, k));
                    if (Globals.IsGoodQCode(item.QCode))
                        goodIntervalSum += interval;
                }
            }

            if (lastTime == null)
                return;

            double prevValue = 0;
            for (var i = 0; i < aux.Count; ++i)
            {
                var cur = aux[i];
                wvalue -= (cur.Item1 - prevValue) * cur.Item2 / 2;
                prevValue = cur.Item1;
            }

            avalue.Write(data, wvalue, goodIntervalSum / totalInterval,
                avalue.IsTakeLastSourceTime() ? lastTime.Value : avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "avgByTrapezFixedStep";
        }
    }
}
