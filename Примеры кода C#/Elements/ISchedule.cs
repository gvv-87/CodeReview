using System;

namespace Aggregator.Elements
{
    public interface ISchedule
    {
        TimeInterval CreateInterval(AggregatedAnalogValue avalue, DateTime time);
        TimeInterval CreateNextInterval(AggregatedAnalogValue avalue, DateTime time);
    }
}
