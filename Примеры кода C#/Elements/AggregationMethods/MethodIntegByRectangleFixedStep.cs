using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodIntegByRectangleFixedStep : IMethod
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
                    var k = interval / 3600;
                    wvalue += item.Value * k;
                    lastTime = item.Time;
                    if (Globals.IsGoodQCode(item.QCode))
                        goodIntervalSum += interval;
                }
            }

            if (lastTime == null)
                return;

            avalue.Write(data, wvalue, goodIntervalSum / totalInterval,
                avalue.IsTakeLastSourceTime() ? lastTime.Value : avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "integByRectangleFixedStep";
        }
    }
}
