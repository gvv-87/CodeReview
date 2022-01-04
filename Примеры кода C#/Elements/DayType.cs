using System;

namespace Aggregator.Elements
{
    public enum DayKind
    {
        Unknown,
        Saturday,
        Holiday,
        Workday,
        DayOff
    }

    public sealed class DayType : IElement
    {
        private static Guid saturdayUid = Guid.Parse("0d6394d1-38d2-489f-acc6-93a743384791");
        private static Guid holidayUid = Guid.Parse("672d58ed-4152-4c0a-b0eb-25192677112e");
        private static Guid workdayUid = Guid.Parse("1000130b-0000-0000-c000-0000006d746c");
        private static Guid dayoffUid = Guid.Parse("1000130a-0000-0000-c000-0000006d746c");

        public DayType(Guid uid, long id, string name)
        {
            Uid = uid;
            Id = id;
            Name = name;

            if (uid == saturdayUid)
                DayKind = DayKind.Saturday;
            else if (uid == holidayUid)
                DayKind = DayKind.Holiday;
            else if (uid == workdayUid)
                DayKind = DayKind.Workday;
            else if (uid == dayoffUid)
                DayKind = DayKind.DayOff;
            else
                DayKind = DayKind.Unknown;
        }

        public DayType(Core core, long id, DayKind dayKind)
        {
            Id = id;

            DayKind = dayKind;
            switch (dayKind)
            {
                case DayKind.Unknown:
                    Uid = Guid.NewGuid();
                    Name = "Неизвестный";
                    break;
                case DayKind.Saturday:
                    Uid = Guid.Parse("0d6394d1-38d2-489f-acc6-93a743384791");
                    Name = "Суббота";
                    break;
                case DayKind.Holiday:
                    Uid = Guid.Parse("672d58ed-4152-4c0a-b0eb-25192677112e");
                    Name = "Праздничный";
                    break;
                case DayKind.Workday:
                    Uid = Guid.Parse("1000130b-0000-0000-c000-0000006d746c");
                    Name = "Рабочий";
                    break;
                case DayKind.DayOff:
                    Uid = Guid.Parse("1000130a-0000-0000-c000-0000006d746c");
                    Name = "Выходной";
                    break;
            }
            core.Elements.Add(id, this);
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }
        public DayKind DayKind { get; private set; }
    }
}
