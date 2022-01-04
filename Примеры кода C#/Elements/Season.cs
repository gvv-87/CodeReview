using System;

namespace Aggregator.Elements
{
    using CIM = Monitel.Mal.Context.CIM16;

    public sealed class Season : IElement
    {
        private bool loaded = false;

        private bool Parse(string text, out int month, out int day)
        {
            if (!string.IsNullOrEmpty(text))
            {
                char[] separator = new char[] { '-' };
                string[] parts = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    if (int.TryParse(parts[0], out month))
                    {
                        if (int.TryParse(parts[1], out day))
                        {
                            if (month >= 1 && month <= 12 && day >= 1 && day <= 31)
                                return true;
                        }
                    }
                }
            }
            month = 0;
            day = 0;
            return false;
        }

        public Season(Guid uid, long id, string name)
        {
            Uid = uid;
            Id = id;
            Name = name;
        }

        public Season(Core core, long id, string name, string startDate, string endDate)
            : this(new Guid((int)id, 0, 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }), id, name)
        {
            StartDate = startDate;
            EndDate = endDate;
            core.Elements.Add(id, this);
        }

        public void Visit(IVisitor visitor)
        {
            visitor.Accept(this);
        }

        /// <summary>
        /// Содержится ли заданное время в сезоне?
        /// </summary>
        /// <param name="time">Локальное время</param>
        public bool Contains(DateTime time)
        {
            var month = time.Month;
            var day = time.Day;

            if (month < StartMonth || month > EndMonth)
                return false;
            if (month == StartMonth && day < StartDay)
                return false;
            if (month == EndMonth && day > EndDay)
                return false;
            return true;
        }

        public void Load(Services.ILogger logger)
        {
            if (loaded)
                return;
            loaded = true;

            try
            {
                if (!Parse(StartDate, out int m, out int d))
                    throw new ElementLoadException($"{nameof(CIM.Season.startDate)} is invalid");
                StartMonth = m;
                StartDay = d;
                if (!Parse(EndDate, out m, out d))
                    throw new ElementLoadException($"{nameof(CIM.Season.endDate)} is invalid");
                EndMonth = m;
                EndDay = d;
            }
            catch (ElementLoadException ex)
            {
                IsValid = false;
                logger.Warning($"ModelBuilder> Error load {nameof(CIM.Season)} {Uid}: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"{nameof(CIM.Season)}: {StartDay}.{StartMonth}..{EndDay}.{EndMonth}";
        }

        public bool IsValid { get; private set; } = true;
        public Guid Uid { get; private set; }
        public long Id { get; private set; }
        public string Name { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int StartMonth { get; private set; }
        public int StartDay { get; private set; }
        public int EndMonth { get; private set; }
        public int EndDay { get; private set; }
    }
}
