using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client
{
    internal class SharebleConst
    {
        /// <summary>
        /// Очередь для сервиса InterlockManager
        /// </summary>
        public const string RmqQueueName = "ha.Services.InterlockManager.Command";

        /// <summary>
        /// Очередь для сервиса InterlockManager2
        /// </summary>
        public const string Interlock2QueueName = "ha.Services.Interlock.Command";
    }
}
