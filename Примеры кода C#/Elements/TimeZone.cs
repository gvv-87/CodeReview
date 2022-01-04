using System;
using System.Collections.Generic;

namespace Aggregator.Elements
{
    using CIM = Monitel.Mal.Context.CIM16;

    public sealed class TimeZone : IElement
    {
        private List<TimeZoneInSeason> seasons;
        private bool loaded = false;

        public TimeZone(Guid uid, long id, string name)
        {
            Uid = uid;
            Id = id;
            Name = name;
        }

        public TimeZone(Core core, long id)
            : this(new Guid((int)id, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), id, $"TimeZone{id}")
        {
            core.Elements.Add(id, this);
        }

        public void Add(TimeZoneInSeason value)
        {
            if (seasons == null)
                seasons = new List<TimeZoneInSeason>(1) { value };
            else
                seasons.Add(value);
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        public TimeInterval CreateInterval(DateTime time)
        {
            if (seasons != null)
            {
                foreach (var s in seasons)
                {
                    var interval = s.CreateInterval(time);
                    if (interval != null)
                        return interval;
                }
            }
            return null;
        }

        public TimeInterval CreateNextInterval(DateTime time)
        {
            if (seasons != null)
            {
                foreach (var s in seasons)
                {
                    var interval = s.CreateNextInterval(time);
                    if (interval != null)
                        return interval;
                }
            }
            return null;
        }

        public void Load(Services.ILogger logger)
        {
            if (loaded)
                return;
            loaded = true;

            try
            {
                if (seasons == null)
                    throw new ElementLoadException("seasons is not defined");
                foreach (var s in seasons)
                {
                    s.Load(logger);
                    if (!s.IsValid)
                        throw new ElementLoadException("season is invalid");
                    IsValid = true;
                }
            }
            catch (ElementLoadException ex)
            {
                IsValid = false;
                logger.Warning($"ModelBuilder> Error load {nameof(CIM.TimeZone)} {Uid}: {ex.Message}");
            }
        }

        public bool IsValid { get; private set; } = true;
        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }

        public bool HasSeasons => seasons != null;
        public IEnumerable<TimeZoneInSeason> GetSeasons()
        {
            return seasons;
        }
    }
}
