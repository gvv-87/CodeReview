using System;

namespace Aggregator.Elements
{
    [Serializable]
    public sealed class ElementLoadException : Exception
    {
        public ElementLoadException(string message) : base(message)
        {
        }

        public ElementLoadException(string entity, string key, string value)
            : base($"{entity} is '{key}' but '{value}' is not defined")
        {
        }
    }
}
