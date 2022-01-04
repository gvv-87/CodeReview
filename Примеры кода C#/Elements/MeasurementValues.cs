using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Aggregator.Elements
{
    public sealed class MeasurementValues : IEnumerable<MeasurementValue>
    {
        private readonly Dictionary<Guid, MeasurementValue> map;

        public MeasurementValues()
        {
            map = new Dictionary<Guid, MeasurementValue>();
        }

        public bool Contains(Guid uid)
        {
            return map.ContainsKey(uid);
        }

        public MeasurementValue Find(Guid uid)
        {
            if (map.TryGetValue(uid, out MeasurementValue p))
                return p;
            return null;
        }

        public void Add(MeasurementValue obj)
        {
            if (obj != null && !Contains(obj.Uid))
                map.Add(obj.Uid, obj);
        }

        public void Remove(Guid uid)
        {
            map.Remove(uid);
        }

        public Guid[] GetUids()
        {
            return map.Keys.ToArray();
        }

        public IEnumerator<MeasurementValue> GetEnumerator()
        {
            return map.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.Values.GetEnumerator();
        }

        public int Count => map.Count;
    }
}
