using System.Collections.Generic;

namespace Aggregator.Elements
{
    public sealed class AbstractElements
    {
        private readonly Dictionary<long, IElement> map;

        public AbstractElements()
        {
            map = new Dictionary<long, IElement>();
        }

        public bool Contains(long id)
        {
            return map.ContainsKey(id);
        }

        public IElement Find(long id)
        {
            if (map.TryGetValue(id, out IElement obj))
                return obj;
            return null;
        }

        public void Add(long id, IElement obj)
        {
            if (obj != null && !map.ContainsKey(id))
                map.Add(id, obj);
        }

        public void Remove(long id)
        {
            map.Remove(id);
        }
    }
}
