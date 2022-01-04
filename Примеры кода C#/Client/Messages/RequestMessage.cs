using Monitel.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterlockManager.Client.Requests
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RequestMessage
    {
        public RequestMessage()
        {
            Header = new Header();
        }
        /// <summary>
        /// Заголовок сообщения
        /// </summary>
        [JsonProperty("header")]
        public Header Header { get; set; }

        [JsonProperty("getObjectInterlocksRequest")]
        public GetObjectInterlocksRequest GetObjectInterlocksRequest { get; set; }

        [JsonProperty("checkActionRequest")]
        public CheckActionRequest CheckActionRequest { get; set; }


        /// <summary>
        /// Метод получения текстового представления
        /// </summary>
        /// <returns>Строковое описание</returns>
        public virtual string GetText()
        {
            return String.Format("RequestMessage_{0}_{1}_{2}", Header.UserName,Header.UserHostName,Header.AppName);
        }

    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GetObjectInterlocksRequest
    {
        /// <summary>
        /// uid объекта
        /// </summary>
        [JsonProperty("uid")]
        public Guid Uid { get; set; }

        ///// <summary>
        ///// является ли запрос от самого же пользователя (?? узнать у Молчанова)
        ///// </summary>
        //[JsonProperty("me")]
        //public bool Me { get; set; }
        
        ///// <summary>
        ///// сессия
        ///// </summary>
        //[JsonProperty("session")]
        //public long Session { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CheckActionRequest
    {
        [JsonProperty("swUid")]
        public string SwUid { get; set; }

        [JsonProperty("command")]
        public double Command { get; set; }

        [JsonProperty("serverContextId")]
        public string ServerContextId { get; set; }

        [JsonProperty("svSources")]
        public string[] SvSources { get; set; }

        [JsonProperty("serverContextConnectionString")]
        public string ServerContextConnectionString { get; set; }
    }
}
