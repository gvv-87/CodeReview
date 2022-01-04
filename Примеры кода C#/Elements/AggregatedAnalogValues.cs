using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Aggregator.Elements
{
    public sealed class AggregatedAnalogValues : IEnumerable<AggregatedAnalogValue>
    {
        private readonly Dictionary<Guid, AggregatedAnalogValue> map;

        public AggregatedAnalogValues()
        {
            map = new Dictionary<Guid, AggregatedAnalogValue>();
        }

        public bool Contains(Guid uid)
        {
            return map.ContainsKey(uid);
        }

        public AggregatedAnalogValue Find(Guid uid)
        {
            if (map.TryGetValue(uid, out AggregatedAnalogValue p))
                return p;
            return null;
        }

        public void Add(AggregatedAnalogValue obj)
        {
            if (obj != null && !Contains(obj.Uid))
                map.Add(obj.Uid, obj);
        }

        public void Remove(AggregatedAnalogValue obj)
        {
            if (obj != null)
                map.Remove(obj.Uid);
        }

        public List<Guid> GetUids()
        {
            return map.Keys.ToList();
        }

        public IEnumerator<AggregatedAnalogValue> GetEnumerator()
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
