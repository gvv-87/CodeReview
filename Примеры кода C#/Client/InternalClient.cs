using RabbitMQ.Client.Events;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InterlockManager.Client
{
    /// <summary>
    /// Класс для работы со службой
    /// </summary>
    internal class InternalClient : BaseClient
    {

        /// <summary>
        /// Объект ожидания
        /// </summary>
        internal class InternalWaitHandler : IWaitHandler
        {
            private readonly BlockingCollection<BasicDeliverEventArgs> _coll = new BlockingCollection<BasicDeliverEventArgs>();
            private readonly InternalClient _client;
            private Exception _currentException;

            /// <summary>
            /// Инициализирует новый экземпляр класса <see cref="InternalWaitHandler"/>.
            /// </summary>
            /// <param name="client"> The client. </param>
            public InternalWaitHandler(InternalClient client)
            {
                _client = client;
            }

            /// <summary>
            /// Возвращает типизированный аналог сообщения
            /// </summary>
            /// <typeparam name="T">Тип сообщения</typeparam>
            /// <returns>Полученное сообщение</returns>
            //public T Get<T>() where T : ResponseMessage
            //{
            //    return _client.ParseMessage<T>(Get());
            //}
            /// <summary>
            /// Возвращает полученное сообщение
            /// </summary>
            /// <returns>Полученное сообщение</returns>
            public BasicDeliverEventArgs Get()
            {
                if (_currentException != null)
                {
                    throw _currentException;
                }
                var retVal = _coll.Take();
                return retVal;
            }

            /// <summary>
            /// Устанализвает факт завершения ожидания
            /// </summary>
            /// <param name="args">Ожидаемый результат</param>
            public void Set(RabbitMQ.Client.Events.BasicDeliverEventArgs args)
            {
                _coll.Add(args);
            }

            /// <summary>
            /// Устанавливает факт ошибки
            /// </summary>
            /// <param name="exp">Произошедшая ошибка</param>
            public void SetException(Exception exp)
            {
                _currentException = exp;
                _coll.CompleteAdding();
            }
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="InternalClient"/>.
        /// </summary>
        /// <param name="connection">
        /// The connection.
        /// </param>
        public InternalClient(RabbitMQ.Client.IConnection connection)
            : base(connection)
        {
        }
        public InternalClient(RabbitMQ.Client.IConnection connection, string commandQueueName)
           : base(connection, commandQueueName)
        {
        }


        public async Task<PLock[]> GetObjectInterlocks(Guid uid, CancellationToken token)
        {
            var req = new Requests.RequestMessage()
            {
                GetObjectInterlocksRequest = new Requests.GetObjectInterlocksRequest()
                {
                    Uid = uid
                },
                Header = new Requests.Header()
                {
                    CorrelationId = Guid.NewGuid().ToString()
                }
            };

            var response = await AsyncWaitableSend(req, token);

            return response.PLocks;
        }

        /// <summary>
        /// Проверить команду на возможность выполнения
        /// </summary>
        /// <param name="command">команда</param>
        /// <param name="serverContextId">id серверного контекста</param>
        /// <param name="svSources">svSources</param>
        /// <param name="swUid">UID sw</param>
        /// <param name="token"></param>
        /// <returns></returns>
        [Obsolete("Метод устарел в части работыс серверным контекстом. serverContextId не будет использоваться")]
        public async Task<PLock[]> CheckActionObsolete(double command, IEnumerable<Guid> svSources, string swUid,CancellationToken token, string serverContextId=null)
        {
            if (svSources == null)
                svSources = new List<Guid>();

            var req = new Requests.RequestMessage()
            {
                CheckActionRequest = new Requests.CheckActionRequest()
                {
                    Command = command,
                    ServerContextId = serverContextId,
                    SvSources = svSources.Select(x=>x.ToString()).ToArray(),
                    SwUid = swUid
                }
               
            };

            var response = await AsyncWaitableSend(req, token);
            return response.PLocks;

        }

        /// <summary>
        /// Проверить команду на возможность выполнения
        /// </summary>
        /// <param name="command">команда</param>
        /// <param name="serverContextConnectionString">строка серверного контекста (malParams.ToString())</param>
        /// <param name="svSources">svSources</param>
        /// <param name="swUid">UID sw</param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<PLock[]> CheckAction(double command, string serverContextConnectionString, IEnumerable<Guid> svSources, string swUid, CancellationToken token)
        {
            if (svSources == null)
                svSources = new List<Guid>();

            var req = new Requests.RequestMessage()
            {
                CheckActionRequest = new Requests.CheckActionRequest()
                {
                    Command = command,
                    ServerContextConnectionString=serverContextConnectionString,
                    SvSources = svSources.Select(x => x.ToString()).ToArray(),
                    SwUid = swUid
                }

            };

            var response = await AsyncWaitableSend(req, token);
            return response.PLocks;

        }
    }

}






