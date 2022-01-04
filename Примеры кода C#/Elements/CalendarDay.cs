using System;

namespace Aggregator.Elements
{
    public sealed class CalendarDay : IElement
    {
        public CalendarDay(Guid uid, long id)
        {
            Uid = uid;
            Id = id;
        }

        public CalendarDay(Core core, int day, int month, int year, DayType dayType)
            : this(new Guid(year, (short)month, (short)day, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }),
                  year * 10000 + month * 100 + day)
        {
            Date = new DateTime(year, month, day);
            DayType = dayType;
            core.Elements.Add(Id, this);
            core.Calendar.Add(this);
            Week = (Date.DayOfYear + 6) / 7;
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public DateTime Date { get; set; }
        public DayType DayType { get; set; }
        public int Week { get; set; }
    }
}
