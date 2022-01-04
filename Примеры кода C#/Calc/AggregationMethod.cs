using System;

namespace Aggregator.Calc
{
    public enum AggregationMethod
    {
        undefined,
        avgArithmetic,
        avgByRectangle,
        avgByTrapez,
        integByRectangle,
        integByTrapez,
        max,
        min,
        sum,
        maxTime,
        minTime,
        Variance,
        StandardDeviation,
        ChangesCount,
        Duration,
        MaxDuration,
        DurationOn,
        DurationOff
    }

    public static class AggregationMethodUids
    {
        public static readonly Guid AvgArithmetic = Guid.Parse("10001632-0000-0000-C000-0000006D746C");
        public static readonly Guid AvgByRectangle = Guid.Parse("10001633-0000-0000-C000-0000006D746C");
        public static readonly Guid AvgByTrapez = Guid.Parse("10001634-0000-0000-C000-0000006D746C");
        public static readonly Guid IntegByRectangle = Guid.Parse("10001635-0000-0000-C000-0000006D746C");
        public static readonly Guid IntegByTrapez = Guid.Parse("10001636-0000-0000-C000-0000006D746C");
        public static readonly Guid Max = Guid.Parse("10001637-0000-0000-C000-0000006D746C");
        public static readonly Guid MaxTime = Guid.Parse("10001638-0000-0000-C000-0000006D746C");
        public static readonly Guid Min = Guid.Parse("10001639-0000-0000-C000-0000006D746C");
        public static readonly Guid MinTime = Guid.Parse("1000163A-0000-0000-C000-0000006D746C");
        public static readonly Guid Sum = Guid.Parse("1000163B-0000-0000-C000-0000006D746C");
        public static readonly Guid Variance = Guid.Parse("1000163C-0000-0000-C000-0000006D746C");
        public static readonly Guid StandardDeviation = Guid.Parse("1000163D-0000-0000-C000-0000006D746C");
        public static readonly Guid ChangesCount = Guid.Parse("1000163E-0000-0000-C000-0000006D746C");
        public static readonly Guid Duration = Guid.Parse("1000163F-0000-0000-C000-0000006D746C");
        public static readonly Guid MaxDuration = Guid.Parse("10001640-0000-0000-C000-0000006D746C");
        public static readonly Guid DurationOn = Guid.Parse("10001641-0000-0000-C000-0000006D746C");
        public static readonly Guid DurationOff = Guid.Parse("10001642-0000-0000-C000-0000006D746C");

        public static AggregationMethod GetEnumFromUid(Guid methodUid)
        {
            return methodUid switch
            {
                var uid when uid == AvgArithmetic => AggregationMethod.avgArithmetic,
                var uid when uid == AvgByRectangle => AggregationMethod.avgByRectangle,
                var uid when uid == AvgByTrapez => AggregationMethod.avgByTrapez,
                var uid when uid == IntegByRectangle => AggregationMethod.integByRectangle,
                var uid when uid == IntegByTrapez => AggregationMethod.integByTrapez,
                var uid when uid == Max => AggregationMethod.max,
                var uid when uid == MaxTime => AggregationMethod.maxTime,
                var uid when uid == Min => AggregationMethod.min,
                var uid when uid == MinTime => AggregationMethod.minTime,
                var uid when uid == Sum => AggregationMethod.sum,
                var uid when uid == Variance => AggregationMethod.Variance,
                var uid when uid == StandardDeviation => AggregationMethod.StandardDeviation,
                var uid when uid == ChangesCount => AggregationMethod.ChangesCount,
                var uid when uid == Duration => AggregationMethod.Duration,
                var uid when uid == MaxDuration => AggregationMethod.MaxDuration,
                var uid when uid == DurationOn => AggregationMethod.DurationOn,
                var uid when uid == DurationOff => AggregationMethod.DurationOff,
                _ => AggregationMethod.undefined,
            };
        }
    }
}
