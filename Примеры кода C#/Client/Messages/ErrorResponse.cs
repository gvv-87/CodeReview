using InterlockManager.Client.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client.Messages
{
    /// <summary>
    /// Класс , описывающий ответ с информации об ошибке
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Конструктор
        /// </summary>
        public ErrorResponse()
        {
            Header = new Header();
        }
        public Header Header { get; set; }
        /// <summary>
        /// Общие ошибки
        /// </summary>
        public static int GeneralError = 0;

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Тип ошибки
        /// </summary>
        public Int32 ErrorType { get; set; }
        
        /// <summary>
        /// Код ошибки
        /// </summary>
        public Int32 Code { get; set; }
    
    }
}
