using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodMinTimeByChange : IMethod
    {
        private readonly Services.Rtdbs.IStatisticalReader reader;

        public MethodMinTimeByChange(AggregatedAnalogValue avalue)
        {
            reader = avalue.Core.Rtdb.CreateMinStatisticalReader();
        }

        public MethodTypeByChange GetMethodTypeByChange()
        {
            return MethodTypeByChange.min;
        }

        public Calc.ICalcData CreateMethodData()
        {
            return new Calc.CalcDataByChange();
        }

        public void Execute(AggregatedAnalogValue avalue, Calc.ICalcData data, TimeInterval interval)
        {
            reader.Read(avalue, data, interval);
        }

        public void Write(AggregatedAnalogValue avalue, Calc.ICalcData data, DateTime from, DateTime to)
        {
            if (from >= to)
                return;

            double extremumValue = double.MaxValue;
            DateTime? extremumTime = null;
            double quota = 0;

            foreach (var ritem in data.Items)
            {
                if (ritem.Time > to)
                    break;

                if (avalue.IsValidStatisticalItem(ritem))
                {
                    var abs = Math.Abs(ritem.Value);
                    if (abs <= extremumValue)
                    {
                        extremumValue = abs;
                        quota = ritem.QualityLevel / 100.0;
                        extremumTime = ritem.Time;
                    }
                }
            }

            if (extremumTime == null)
                return;

            avalue.Write(data, MethodExtensions.GetWritedTime(extremumTime.Value), quota, avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "minTimeByChange";
        }
    }
}
