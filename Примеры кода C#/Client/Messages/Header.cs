using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client.Requests
{
    public class Header
    {
        /// <summary>
        /// Идентификатор процесса
        /// </summary>
        public string CorrelationId {get;set;}

        /// <summary>
        /// Имя очереди для ответа
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Имя хоста пользователя
        /// </summary>
        public string UserHostName { get; set; }
        /// <summary>
        /// Имя приложения
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Тег, используется только на серверной стороне для квитирования сообщений
        /// </summary>
        public ulong DeliveryTag { get; set; }

        /// <summary>
        /// Тип сообщения
        /// </summary>
        public Int32 MessageType { get; set; }

       
    }
}
