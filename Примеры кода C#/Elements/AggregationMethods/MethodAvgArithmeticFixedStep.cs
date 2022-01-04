using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodAvgArithmeticFixedStep : IMethod
    {
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
            int count = 0;

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
                    wvalue += item.Value;
                    lastTime = item.Time;
                    ++count;
                    if (Globals.IsGoodQCode(item.QCode))
                    {
                        DateTime tnext = (i == items.Count - 1) ? to : items[i + 1].Time;
                        goodIntervalSum += tnext.Subtract(item.Time).TotalSeconds;
                    }
                }
            }

            if (lastTime == null)
                return;

            avalue.Write(data, wvalue / count, goodIntervalSum / totalInterval,
                avalue.IsTakeLastSourceTime() ? lastTime.Value : avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "avgArithmeticFixedStep";
        }
    }
}
