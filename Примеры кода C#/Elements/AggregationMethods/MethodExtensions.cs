using System;

namespace Aggregator.Elements.AggregationMethods
{
    internal static class MethodExtensions
    {
        public static void Write(this AggregatedAnalogValue avalue, Calc.ICalcData data,
            double value, double quality, DateTime time)
        {
            var witem = new ValueDataItem(avalue.Uid, avalue.Inverse ? -value : value, Globals.GetResultQCode(quality), time);
            if (data.CanWrite(avalue, witem))
            {
                avalue.Core.Logger.Debug($"Write {witem}");
                avalue.Core.Rtdb.Write(witem);
            }
        }

        public static double GetWritedTime(DateTime time)
        {
            return ((DateTimeOffset)time).ToUnixTimeSeconds();
        }
    }
}
