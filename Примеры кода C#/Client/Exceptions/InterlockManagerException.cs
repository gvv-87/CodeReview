using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client.Exceptions
{
    public class InterlockManagerException:Exception
    {
        public InterlockManagerException() : base()
        {
        }

        public InterlockManagerException(string message) : base(message)
        {
        }

        public InterlockManagerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
