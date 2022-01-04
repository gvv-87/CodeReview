using System;

namespace Aggregator.Elements
{
    using CIM = Monitel.Mal.Context.CIM16;

    public sealed class HISPartition : IElement
    {
        private bool loaded = false;

        private static DateTime NextStepMinute(DateTime time, int step)
        {
            var rem = time.Minute % step;
            return (rem == 0) ? time : time.AddMinutes(rem);
        }

        private static DateTime NextStepHour(DateTime time)
        {
            return (time.Minute == 0) ? time : time.AddHours(1);
        }

        private static DateTime NextStepDay(DateTime time)
        {
            var hour = time.Hour;
            var minute = time.Minute;
            if (hour == 0)
                return (minute == 0) ? time : time.AddMinutes(-minute).AddDays(1);
            if (minute == 0)
                return time.AddHours(-hour).AddDays(1);
            return time.AddMinutes(-minute).AddHours(-hour).AddDays(1);
        }

        private static DateTime PrevStepDay(DateTime time)
        {
            var hour = time.Hour;
            var minute = time.Minute;
            if (hour == 0)
                return (minute == 0) ? time.AddDays(-1) : time.AddMinutes(-minute);
            if (minute == 0)
                return time.AddHours(-hour);
            return time.AddMinutes(-minute).AddHours(-hour);
        }

        private static DateTime NextStepMonth(DateTime time)
        {
            if (time.Day == 1 && time.Hour == 0 && time.Minute == 0)
                return time;
            return new DateTime(time.Year, time.Month, 1, 0, 0, 0, DateTimeKind.Local).AddMonths(1);
        }

        private static DateTime PrevStepMonth(DateTime time)
        {
            if (time.Day == 1 && time.Hour == 0 && time.Minute == 0)
                return time.AddMonths(-1);
            return new DateTime(time.Year, time.Month, 1, 0, 0, 0, DateTimeKind.Local);
        }

        public HISPartition(Guid uid, long id, string name)
        {
            Uid = uid;
            Id = id;
            Name = name;
        }

        public HISPartition(Core core, string uid, long id, string name, CIM.RTDBDataType dataType, CIM.HISStorageStep? step)
            :
            this(Guid.Parse(uid), id, name)
        {
            DataType = dataType;
            Step = step;
            core.Elements.Add(id, this);
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public void Load(string name)
        {
            if (loaded)
                return;

            loaded = true;
            switch (DataType)
            {
                case CIM.RTDBDataType.byChange:
                case CIM.RTDBDataType.byChangeOneTime:
                    StepIntervalInSeconds = 0;
                    return;
                case CIM.RTDBDataType.fixedStep:
                case CIM.RTDBDataType.fixedStepWithTime:
                    if (Step == null)
                        break;
                    StepIntervalInSeconds = (int)Step.Value;
                    return;
                default:
                    throw new ElementLoadException(
                        $"{name} has unknown '{nameof(CIM.HISPartition.dataType)}'");
            }
            throw new ElementLoadException(
                name + "." + nameof(CIM.HISPartition.dataType),
                DataType.ToString(),
                nameof(CIM.HISPartition.step));
        }

        /// <summary>
        /// Получение времени ближайшего шага, расположенного в данном времени или в ближайшем будущем
        /// </summary>
        /// <param name="time">Время, выровненное до минуты</param>
        /// <returns>Заданное время time и ближайшее будущее время шага</returns>
        public DateTime GetNearStepTime(DateTime time)
        {
            if (!IsByChange && Step != null)
            {
                switch (Step.Value)
                {
                    case CIM.HISStorageStep.minute1:
                        return NextStepMinute(time, 1);
                    case CIM.HISStorageStep.minute3:
                        return NextStepMinute(time, 3);
                    case CIM.HISStorageStep.minute5:
                        return NextStepMinute(time, 5);
                    case CIM.HISStorageStep.minute10:
                        return NextStepMinute(time, 10);
                    case CIM.HISStorageStep.minute15:
                        return NextStepMinute(time, 15);
                    case CIM.HISStorageStep.minute30:
                        return NextStepMinute(time, 30);
                    case CIM.HISStorageStep.hour1:
                        return NextStepHour(time);
                    case CIM.HISStorageStep.day1:
                        return NextStepDay(time);
                    case CIM.HISStorageStep.month1:
                        return NextStepMonth(time);
                }
            }
            return time;
        }

        /// <summary>
        /// Получение времени записи
        /// </summary>
        /// <param name="time">Конец интервала</param>
        public DateTime GetResultWriteTime(DateTime time)
        {
            if (!IsByChange && Step != null)
            {
                switch (Step.Value)
                {
                    case CIM.HISStorageStep.minute1:
                        return time;
                    case CIM.HISStorageStep.minute3:
                        return time;
                    case CIM.HISStorageStep.minute5:
                        return time;
                    case CIM.HISStorageStep.minute10:
                        return time;
                    case CIM.HISStorageStep.minute15:
                        return time;
                    case CIM.HISStorageStep.minute30:
                        return time;
                    case CIM.HISStorageStep.hour1:
                        return time;
                    case CIM.HISStorageStep.day1:
                        return PrevStepDay(time);
                    case CIM.HISStorageStep.month1:
                        return PrevStepMonth(time);
                }
            }
            return time;
        }

        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }
        public CIM.RTDBDataType DataType { get; set; }
        public CIM.HISStorageStep? Step { get; set; }
        public int StepIntervalInSeconds { get; private set; } // 0 соответствует типу byChange
        public bool IsByChange => StepIntervalInSeconds == 0;
        public bool IsFixedStep => StepIntervalInSeconds != 0;
    }
}
