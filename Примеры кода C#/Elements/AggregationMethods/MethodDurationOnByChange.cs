using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodDurationOnByChange : IMethod
    {
        private readonly Services.Rtdbs.IStatisticalReader reader;

        public MethodDurationOnByChange(AggregatedAnalogValue avalue)
        {
            reader = avalue.Core.Rtdb.CreateDurationOnStatisticalReader();
        }

        public MethodTypeByChange GetMethodTypeByChange()
        {
            return MethodTypeByChange.durationOn;
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
            int count = 0;
            double sum = 0;
            double m_base = 0.000001;
            double m_quality = 1.0;
            DateTime prevTime = from;
            foreach (var ritem in data.Items)
            {
                double v = ritem.Value;
                double q = (ritem.QualityLevel <= 100) ? ritem.QualityLevel / 100.0 : 0;
                sum += v;
                double tDiff = ritem.Time.Subtract(prevTime).TotalSeconds;
                m_quality = (m_base * m_quality + tDiff * q) / (m_base + tDiff);
                m_base += tDiff;
                prevTime = ritem.Time;
                ++count;
            }

            if (count == 0)
                return;

            avalue.Write(data, sum, m_quality, avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "durationOnByChange";
        }
    }
}
