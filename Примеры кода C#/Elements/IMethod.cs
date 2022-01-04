using System;

namespace Aggregator.Elements
{
    public enum MethodTypeByChange : byte
    {
        none = 0,
        min = 1,
        max = 2,
        integ = 3,
        avg = 4,
        count,
        deviation,
        variance,
        duration,
        maxDuration,
        durationOn,
        durationOff
    }

    public interface IMethod
    {
        MethodTypeByChange GetMethodTypeByChange();
        Calc.ICalcData CreateMethodData();
        void Execute(AggregatedAnalogValue avalue, Calc.ICalcData data, TimeInterval interval);
        void Write(AggregatedAnalogValue avalue, Calc.ICalcData data, DateTime from, DateTime to);
    }
}
