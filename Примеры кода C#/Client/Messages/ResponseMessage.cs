using InterlockManager.Client.Messages;
using Monitel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client.Requests
{
    public class ResponseMessage
    {
        public ResponseMessage()
        {
            Header = new Header();
        }
        /// <summary>
      /// Заголовок сообщения
      /// </summary>
        public Header Header { get; set; }

        /// <summary>
        /// Массив блокировок
        /// </summary>
        public PLock[] PLocks { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public ErrorResponse ErrorResponse { get; set; }

    }
}
