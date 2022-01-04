using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client.Attributes
{
    /// <summary>
    /// Атрибут для обеспечения методов обработки событий
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageOperatorAttribute : Attribute
    {
    }
}
