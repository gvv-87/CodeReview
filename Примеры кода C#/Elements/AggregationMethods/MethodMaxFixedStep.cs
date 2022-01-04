using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodMaxFixedStep : IMethod
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
            double amax = -1;
            double wvalue = 0;
            double goodIntervalSum = 0;
            DateTime? lastTime = null;

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
                    var abs = Math.Abs(item.Value);
                    if (abs >= amax)
                    {
                        amax = abs;
                        wvalue = item.Value;
                    }
                    lastTime = item.Time;
                    if (Globals.IsGoodQCode(item.QCode))
                    {
                        DateTime tend = (i == items.Count - 1) ? to : items[i + 1].Time;
                        goodIntervalSum += tend.Subtract(item.Time).TotalSeconds;
                    }
                }
            }

            if (lastTime == null)
                return;

            avalue.Write(data, wvalue, goodIntervalSum / totalInterval,
                avalue.IsTakeLastSourceTime() ? lastTime.Value : avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "maxFixedStep";
        }
    }
}
