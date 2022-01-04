﻿using System;

namespace Aggregator.Elements.AggregationMethods
{
    public sealed class MethodSumFixedStep : IMethod
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
            if (items.Count == 0)
                return;

            double totalInterval = to.Subtract(from).TotalSeconds;
            if (totalInterval < 0.1)
                return;

            double wvalue = 0;
            double goodIntervalSum = 0;
            DateTime? lastTime = null;
            avalue.Core.Logger.Debug($"Before write {avalue.Uid}: from {DateTimeUtils.ToString(from)}, to {DateTimeUtils.ToString(to)}");
            foreach (var item in items)
                avalue.Core.Logger.Debug($"  => {item}");

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                if (item.Time >= to)
                    break;
                if (avalue.IsProcessed(item.QCode, item.Value))
                {
                    wvalue += item.Value;
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
            return "sumFixedStep";
        }
    }
}
