using System;
using System.Collections.Generic;

namespace Aggregator.Elements
{
    using CIM = Monitel.Mal.Context.CIM16;

    public sealed class AggregatedAnalogValue : IElement
    {
        private Dictionary<Guid, DayType> dayTypes;
        private AggregatedAnalogValueData data;

        private IMethod CreateMethod()
        {
            IMethod res = null;

            switch (AggregationMethodType)
            {
                case Calc.AggregationMethod.avgArithmetic:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodAvgByChange(this);
                    else
                        res = new AggregationMethods.MethodAvgArithmeticFixedStep();
                    break;
                case Calc.AggregationMethod.avgByRectangle:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodAvgByChange(this);
                    else
                        res = new AggregationMethods.MethodAvgByRectangleFixedStep();
                    break;
                case Calc.AggregationMethod.avgByTrapez:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodAvgByChange(this);
                    else
                        res = new AggregationMethods.MethodAvgByTrapezFixedStep();
                    break;
                case Calc.AggregationMethod.integByRectangle:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodIntegByChange(this);
                    else
                        res = new AggregationMethods.MethodIntegByRectangleFixedStep();
                    break;
                case Calc.AggregationMethod.integByTrapez:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodIntegByChange(this);
                    else
                        res = new AggregationMethods.MethodIntegByTrapezFixedStep();
                    break;
                case Calc.AggregationMethod.max:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodMaxByChange(this);
                    else
                        res = new AggregationMethods.MethodMaxFixedStep();
                    break;
                case Calc.AggregationMethod.min:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodMinByChange(this);
                    else
                        res = new AggregationMethods.MethodMinFixedStep();
                    break;
                case Calc.AggregationMethod.sum:
                    if (!Source.IsByChange)
                        res = new AggregationMethods.MethodSumFixedStep();
                    break;
                case Calc.AggregationMethod.maxTime:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodMaxTimeByChange(this);
                    else
                        res = new AggregationMethods.MethodMaxTimeFixedStep();
                    break;
                case Calc.AggregationMethod.minTime:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodMinTimeByChange(this);
                    else
                        res = new AggregationMethods.MethodMinTimeFixedStep();
                    break;
                case Calc.AggregationMethod.Variance:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodVarianceByChange(this);
                    break;
                case Calc.AggregationMethod.StandardDeviation:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodDeviationByChange(this);
                    break;
                case Calc.AggregationMethod.ChangesCount:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodCountByChange(this);
                    break;
                case Calc.AggregationMethod.Duration:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodDurationByChange(this);
                    break;
                case Calc.AggregationMethod.MaxDuration:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodMaxDurationByChange(this);
                    break;
                case Calc.AggregationMethod.DurationOn:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodDurationOnByChange(this);
                    break;
                case Calc.AggregationMethod.DurationOff:
                    if (Source.IsByChange)
                        res = new AggregationMethods.MethodDurationOffByChange(this);
                    break;
                default:
                    throw new ElementLoadException($"unknow {nameof(CIM.AggregatedAnalogValue.Method)}: {AggregationMethodType}");
            }

            if (res == null)
                throw new ElementLoadException($"not applicable {nameof(CIM.AggregatedAnalogValue.Method)}: {AggregationMethodType}");

            return res;
        }

        private ISchedule CreateShedule(Services.ILogger logger)
        {
            string valueName;
            switch (ScheduleType)
            {
                case CIM.AggregationSchedule.day:
                    return new AggregationSchedules.ScheduleDay();
                case CIM.AggregationSchedule.energyWeek:
                    if (dayTypes == null)
                        return new AggregationSchedules.ScheduleWeek();
                    else
                        return new AggregationSchedules.ScheduleEnergyWeek();
                case CIM.AggregationSchedule.interval:
                    if (IntervalStart == null)
                        IntervalStart = new DateTime(0);
                    if (IntervalEnd == null)
                        IntervalEnd = new DateTime(0);
                    if (IntervalStart.Value.Ticks == IntervalEnd.Value.Ticks)
                        throw new ElementLoadException($"{nameof(CIM.AggregatedAnalogValue.intervalStart)} and {nameof(CIM.AggregatedAnalogValue.intervalEnd)} are equal");
                    return new AggregationSchedules.ScheduleInterval(IntervalStart.Value, IntervalEnd.Value);
                case CIM.AggregationSchedule.month:
                    return new AggregationSchedules.ScheduleMonth();
                case CIM.AggregationSchedule.quarter:
                    return new AggregationSchedules.ScheduleQuarter();
                case CIM.AggregationSchedule.regular:
                    if (RegularPeriod == null)
                    {
                        valueName = nameof(CIM.AggregatedAnalogValue.regularPeriod);
                        break;
                    }
                    int period = (int)RegularPeriod.Value;
                    switch (period)
                    {
                        case 60:
                        case 60 * 2:
                        case 60 * 3:
                        case 60 * 5:
                        case 60 * 10:
                        case 60 * 15:
                        case 60 * 20:
                        case 60 * 30:
                        case 3600:
                        case 3600 * 2:
                        case 3600 * 3:
                        case 3600 * 4:
                        case 3600 * 6:
                        case 3600 * 8:
                        case 3600 * 12:
                            break;
                        default:
                            throw new ElementLoadException($"Invalid {nameof(CIM.AggregatedAnalogValue.regularPeriod)}");
                    }
                    return new AggregationSchedules.ScheduleRegular(period);
                case CIM.AggregationSchedule.timeZone:
                    if (TimeZone == null)
                    {
                        valueName = nameof(CIM.AggregatedAnalogValue.TimeZone);
                        break;
                    }
                    if (!TimeZone.IsValid)
                    {
                        throw new ElementLoadException(
                            $"{nameof(CIM.AggregatedAnalogValue.TimeZone)} is invalid");
                    }
                    TimeZone.Load(logger);
                    if (!TimeZone.IsValid)
                    {
                        throw new ElementLoadException(
                            $"{nameof(CIM.AggregatedAnalogValue.TimeZone)} is invalid");
                    }
                    return new AggregationSchedules.ScheduleTimeZone();
                case CIM.AggregationSchedule.year:
                    return new AggregationSchedules.ScheduleYear();
                default:
                    throw new ElementLoadException(
                        $"unknown {nameof(CIM.AggregatedAnalogValue.schedule)}");
            }

            throw new ElementLoadException(
                nameof(CIM.AggregatedAnalogValue.schedule),
                ScheduleType.ToString(),
                valueName);
        }

        private IValueFilter CreateValueFilter()
        {
            switch (ValueFilterType)
            {
                case CIM.AggregationValueFilterType.all:
                    return new AggregationValueFilterTypes.ValueFilterAll();
                case CIM.AggregationValueFilterType.ge:
                    if (ValueFilterValue == null)
                        break;
                    return new AggregationValueFilterTypes.ValueFilterGreaterOrEqual(ValueFilterValue.Value);
                case CIM.AggregationValueFilterType.gt:
                    if (ValueFilterValue == null)
                        break;
                    return new AggregationValueFilterTypes.ValueFilterGreater(ValueFilterValue.Value);
                case CIM.AggregationValueFilterType.le:
                    if (ValueFilterValue == null)
                        break;
                    return new AggregationValueFilterTypes.ValueFilterLessOrEqual(ValueFilterValue.Value);
                case CIM.AggregationValueFilterType.lt:
                    if (ValueFilterValue == null)
                        break;
                    return new AggregationValueFilterTypes.ValueFilterLess(ValueFilterValue.Value);
                case CIM.AggregationValueFilterType.negative:
                    return new AggregationValueFilterTypes.ValueFilterNegative();
                case CIM.AggregationValueFilterType.positive:
                    return new AggregationValueFilterTypes.ValueFilterPositive();
                default:
                    return new AggregationValueFilterTypes.ValueFilterAll();
            }

            throw new ElementLoadException(
                nameof(CIM.AggregatedAnalogValue.valueFilterType),
                ValueFilterType.ToString(),
                nameof(CIM.AggregatedAnalogValue.valueFilterValue));
        }

        private void LoadHISPartition()
        {
            string name = nameof(CIM.AggregatedAnalogValue.HISPartition);
            if (HISPartition == null)
                throw new ElementLoadException($"{name} is not defined");
            HISPartition.Load(name);
            StepIntervalInSeconds = HISPartition.StepIntervalInSeconds;
        }

        private void LoadSource(Services.ILogger logger)
        {
            if (Source == null)
                throw new ElementLoadException($"{nameof(CIM.AggregatedAnalogValue.Source)} is not defined");
            Source.Load(logger, this);
            if (!Source.IsValid)
                throw new ElementLoadException($"{nameof(CIM.AggregatedAnalogValue.Source)} is invalid");
        }

        public AggregatedAnalogValue(Core core, Guid uid, long id, string name)
        {
            Core = core;
            Uid = uid;
            Id = id;
            Name = name;
            data = new AggregatedAnalogValueData();
        }

        public AggregatedAnalogValue(Core core, long id,
            Calc.AggregationMethod aggregationMethodType,
            CIM.AggregationSchedule scheduleType,
            CIM.AggregationValueFilterType valueFilterType,
            double? intermediateCalcStep,
            double? queryStep,
            double? regularPeriod,
            DateTime? intervalStart,
            DateTime? intervalEnd,
            bool inverse,
            long? qcFilterValue,
            double? valueFilterValue,
            HISPartition hisPartition,
            MeasurementValue source,
            TimeZone timeZone,
            params DayType[] dayTypes)
            :
            this(core, new Guid((int)id, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), id, $"AggregatedAnalogValue{id}")
        {
            AggregationMethodType = aggregationMethodType;
            ScheduleType = scheduleType;
            ValueFilterType = valueFilterType;
            IntermediateCalcStep = intermediateCalcStep;
            QueryStep = queryStep;
            RegularPeriod = regularPeriod;
            IntervalStart = intervalStart;
            IntervalEnd = intervalEnd;
            Inverse = inverse;
            QcFilterValue = qcFilterValue;
            ValueFilterValue = valueFilterValue;
            HISPartition = hisPartition;
            Source = source;
            TimeZone = timeZone;
            foreach (var p in dayTypes)
                Add(p);

            Load();
            core.Elements.Add(id, this);
            core.AggregatedAnalogValues.Add(this);
        }

        public void Update(AggregatedAnalogValue exist)
        {
            if (exist == null)
                return;
            data = exist.data;
            exist.data = null;
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public void Add(DayType obj)
        {
            if (obj == null)
                return;
            if (dayTypes == null)
                dayTypes = new Dictionary<Guid, DayType>();
            dayTypes[obj.Uid] = obj;
        }

        public bool Contains(DayType obj)
        {
            if (obj == null)
                return false;
            if (dayTypes == null)
                return false;
            return dayTypes.ContainsKey(obj.Uid);
        }

        public void Load()
        {
            try
            {
                Schedule = CreateShedule(Core.Logger);
                ValueFilter = CreateValueFilter();
                LoadHISPartition();
                LoadSource(Core.Logger);
                AggregationMethod = CreateMethod();
                if (Source.StepIntervalInSeconds == 0)
                {
                    if (AggregationMethod.GetMethodTypeByChange() == MethodTypeByChange.none)
                    {
                        throw new ElementLoadException(string.Format("{0} '{1}' is not applicable for source 'byChange'",
                            nameof(CIM.AggregatedAnalogValue.Method),
                            AggregationMethod.ToString()));
                    }
                    if (!ValueFilter.CanApplyByChange())
                    {
                        throw new ElementLoadException(string.Format("{0} '{1}' is not applicable for source 'byChange'",
                            nameof(CIM.AggregatedAnalogValue.valueFilterType),
                            ValueFilterType.ToString()));
                    }
                }
                if (TimeZone != null)
                    TimeZone.Load(Core.Logger);
                //QCodeFilter = QcFilterValue == null ? 0 : (uint)QcFilterValue;
                IsValid = true;
            }
            catch (ElementLoadException ex)
            {
                IsValid = false;
                Core.Logger.Warning($"ModelBuilder> Error load {nameof(CIM.AggregatedAnalogValue)} {Uid}: {ex.Message}");
            }
        }

        public void OnRemove()
        {
            if (Source != null)
                Source.RemoveDependency(this);
        }

        public void CreateRealtimeInterval(DateTime time)
        {
            if (Source == null || HISPartition == null)
                return;
            var interval = Schedule.CreateInterval(this, time);
            TimeInterval nextInterval = null;
            if (interval == null)
                nextInterval = Schedule.CreateNextInterval(this, time);
            var step = GetIntermediateStep();
            data.CreateRealtimeInterval(this, interval, nextInterval, step);
        }

        public DateTime GetNearStepTime(DateTime time)
        {
            return HISPartition != null ? HISPartition.GetNearStepTime(time) : time;
        }

        public void ExecuteRealtime(TimeInterval interval)
        {
            data.ExecuteRealtime(this, interval);
        }

        public void ExecuteRecalc()
        {
            data.ExecuteRecalc(this);
        }

        public void OnInitialize(DateTime lastTime)
        {
            LastStartTime = lastTime;
        }

        public void AddRecalcInterval(DateTime from, DateTime? to, DateTime? start)
        {
            data.AddRecalcInterval(this, from, to, start);
        }

        public void OnReadSubscriptionMeasurementValue(ValueDataItem item)
        {
            if (Core.SingleAValue != null)
            {
                if (Core.SingleAValue != this)
                    return;
            }
            data.OnValueSubscriptionReceived(this, item);
        }

        public DateTime GetResultWriteTime(DateTime time)
        {
            return (HISPartition == null) ? time : HISPartition.GetResultWriteTime(time);
        }

        public bool IsTakeLastSourceTime()
        {
            return HISPartition != null && HISPartition.DataType == CIM.RTDBDataType.fixedStepWithTime;
        }

        public int GetIntermediateStep()
        {
            var result = IntermediateCalcStep != null ? (int)IntermediateCalcStep.Value : 0;
            if (result < 60 && HISPartition != null)
            {
                result = HISPartition.StepIntervalInSeconds;
                if (result < 60 && Source != null && Source.HISPartition != null)
                    result = Source.HISPartition.StepIntervalInSeconds;
            }
            return result > 60 ? DateTimeUtils.AlignToMinute(result) : 60;
        }

        public int GetIntervalStep()
        {
            if (QueryStep != null)
                return (int)QueryStep.Value;
            else if (Source != null && Source.StepIntervalInSeconds != 0)
                return Source.StepIntervalInSeconds;
            return 0;
        }

        public ulong GetQCodeFilter()
        {
            return QcFilterValue == null ? 0 : (ulong)QcFilterValue.Value;
        }

        public bool IsProcessed(uint qcode, double value)
        {
            return !Globals.IsIgnoredQCode(qcode)
                && IsPassQCodeFilter(qcode)
                && double.IsFinite(value)
                && Math.Abs(value) < 1e20
                && ValueFilter.IsValid(value);
        }

        public bool IsPassQCodeFilter(uint qcode)
        {
            return (QcFilterValue != null && (uint)QcFilterValue.Value != 0)
                ? (qcode & (uint)QcFilterValue.Value) == 0
                : true;
        }

        public bool IsValidItem(ValueDataItem item)
        {
            if (!item.IsGood())
                return false;
            if (!item.IsFinite())
                return false;
            var qcfilter = GetQCodeFilter();
            if (qcfilter != 0 && (item.QCode & qcfilter) == 0)
                return false;
            if (!ValueFilter.IsValid(item.Value))
                return false;
            return true;
        }

        public bool IsValidStatisticalItem(ValueDataItem item)
        {
            if (item.QualityLevel < Globals.Quota)
                return false;
            if (!ValueFilter.IsValid(item.Value))
                return false;
            return true;
        }

        public Calc.RecalcManager RecalcManager => data?.RecalcManager;

        public bool HasDayTypes => dayTypes != null;

        public IEnumerable<DayType> GetDayTypes()
        {
            if (dayTypes != null)
                return dayTypes.Values;
            return null;
        }

        public DateTime? LastStartTime
        {
            get { return data.LastStartTime; }
            set { data.LastStartTime = value; }
        }

        public bool HasRecalcIntervals => data.HasRecalcIntervals;

        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }
        public Calc.AggregationMethod AggregationMethodType { get; set; }
        public CIM.AggregationSchedule ScheduleType { get; set; }
        public CIM.AggregationValueFilterType ValueFilterType { get; set; }
        public double? IntermediateCalcStep { get; set; }
        public double? QueryStep { get; set; }
        public double? RegularPeriod { get; set; }
        public DateTime? IntervalStart { get; set; }
        public DateTime? IntervalEnd { get; set; }
        public bool Inverse { get; set; }
        public long? QcFilterValue { get; set; }
        public double? ValueFilterValue { get; set; }
        public HISPartition HISPartition { get; set; }
        public MeasurementValue Source { get; set; }
        public TimeZone TimeZone { get; set; }

        public Core Core { get; private set; }
        public IMethod AggregationMethod { get; private set; }
        public ISchedule Schedule { get; private set; }
        public IValueFilter ValueFilter { get; private set; }
        public int StepIntervalInSeconds { get; private set; }
        //public uint QCodeFilter { get; private set; }
        public bool IsValid { get; private set; } = false;
    }
}
