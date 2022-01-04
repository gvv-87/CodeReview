using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodMinByChange : IMethod
    {
        private readonly Services.Rtdbs.IStatisticalReader reader;

        public MethodMinByChange(AggregatedAnalogValue avalue)
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
            double quota = -1;
            double wvalue = 0;

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
                        wvalue = ritem.Value;
                        quota = ritem.QualityLevel / 100.0;
                    }
                }
            }

            if (quota < 0)
                return;

            avalue.Write(data, wvalue, quota, avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "minByChange";
        }
    }
}
