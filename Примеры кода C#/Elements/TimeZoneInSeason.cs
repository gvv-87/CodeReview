using System;

namespace Aggregator.Elements
{
    using CIM = Monitel.Mal.Context.CIM16;

    public sealed class TimeZoneInSeason : IElement
    {
        private bool loaded = false;
        private DayTimeRange dayTimeRange;

        public TimeZoneInSeason(Guid uid, long id)
        {
            Uid = uid;
            Id = id;
        }

        public TimeZoneInSeason(Core core, long id,
            DateTime startTime, DateTime endTime, Season season)
            : this(new Guid((int)id, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), id)
        {
            StartTime = startTime;
            EndTime = endTime;
            Season = season;
            core.Elements.Add(id, this);
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public TimeInterval CreateInterval(DateTime time)
        {
            if (dayTimeRange == null)
                return null;
            if (!Season.Contains(time))
                return null;
            return dayTimeRange.CreateInterval(time);
        }

        public TimeInterval CreateNextInterval(DateTime time)
        {
            if (dayTimeRange == null)
                return null;
            if (!Season.Contains(time))
                return null;
            return dayTimeRange.CreateNextInterval(time);
        }

        public void Load(Services.ILogger logger)
        {
            if (loaded)
                return;
            loaded = true;

            try
            {
                //if (StartTime == null)
                //    throw new ElementLoadException(nameof(CIM.TimeZoneInSeason.startTime) + " is not defined");
                //if (EndTime == null)
                //    throw new ElementLoadException(nameof(CIM.TimeZoneInSeason.endTime) + " is not defined");
                //if (StartTime.Ticks == EndTime.Ticks)
                //    throw new ElementLoadException($"{nameof(CIM.TimeZoneInSeason.startTime)} and {nameof(CIM.TimeZoneInSeason.endTime)} are equal");
                //if (Season == null)
                //    throw new ElementLoadException($"{nameof(CIM.TimeZoneInSeason.Season)} is not defined");
                //if (!Season.IsValid)
                //    throw new ElementLoadException($"{nameof(CIM.TimeZoneInSeason.Season)} is invalid");
                Season.Load(logger);
                //if (!Season.IsValid)
                //    throw new ElementLoadException($"{nameof(CIM.TimeZoneInSeason.Season)} is invalid");
                dayTimeRange = new DayTimeRange(StartTime, EndTime);
                IsValid = true;
            }
            catch (ElementLoadException)
            {
                IsValid = false;
                //logger.Warning($"ModelBuilder> Error load {nameof(CIM.TimeZoneInSeason)} {Uid}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return "TimeZoneInSeason"; // nameof(CIM.TimeZoneInSeason);
        }

        public bool IsValid { get; private set; } = true;
        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public Season Season { get; set; }
    }
}
