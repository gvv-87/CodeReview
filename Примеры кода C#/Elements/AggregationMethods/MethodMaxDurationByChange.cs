﻿using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodMaxDurationByChange : IMethod
    {
        private readonly Services.Rtdbs.IStatisticalReader reader;

        public MethodMaxDurationByChange(AggregatedAnalogValue avalue)
        {
            reader = avalue.Core.Rtdb.CreateMaxDurationStatisticalReader();
        }

        public MethodTypeByChange GetMethodTypeByChange()
        {
            return MethodTypeByChange.maxDuration;
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
            if (data.Items.Count == 0)
                return;

            double wvalue;
            double quality;

            if (data.Items.Count > 1)
            {
                wvalue = 0;
                quality = 0;
            }
            else
            {
                wvalue = data.Items[0].Value;
                quality = 1;
            }

            avalue.Write(data, wvalue, quality, avalue.GetResultWriteTime(to));
        }

        public override string ToString()
        {
            return "maxDurationByChange";
        }
    }
}
