using InterlockManager.Client.Exceptions;
using InterlockManager.Client.Requests;
using InterlockManager.ShareConstants;
using Monitel.RabbitMQ.Infrastructure;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InterlockManager.Client
{
    /// <summary>
    /// Базовый клиент для RCP работы с RabbitMq
    /// </summary>
    internal abstract class BaseClient : IDisposable
    {
        /// <summary>
        /// Интерфейс объекта ожидания
        /// </summary>
        internal interface IWaitHandler
        {
            /// <summary>
            /// Устанавливает факт завершения ожидания
            /// </summary>
            /// <param name="args">Ожидаемый результат</param>
            void Set(BasicDeliverEventArgs args);

            /// <summary>
            /// Устанавливает факт ошибки
            /// </summary>
            /// <param name="exp">Произошедшая ошибка</param>
            void SetException(Exception exp);
        }

        /// <summary>
        /// Класс реализующий объект ожидания
        /// </summary>
        private class WaitDesc : IWaitHandler
        {
            readonly TaskCompletionSource<BasicDeliverEventArgs> _wait = new TaskCompletionSource<BasicDeliverEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            /// <summary>
            /// Асинхронное ожидание
            /// </summary>
            /// <param name="token"> Объект отмены операции </param>
            /// <returns> Объект ожидания </returns>
            public Task<BasicDeliverEventArgs> WaitAsync(CancellationToken token)
            {
                token.Register(delegate
                {
                    try { _wait.SetCanceled(); }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message} {ex.StackTrace}");
                    }
                });
                return _wait.Task;
            }
            /// <summary>
            /// Устанавливает факт завершения ожидания
            /// </summary>
            /// <param name="args">Ожидаемый результат</param>
            public void Set(BasicDeliverEventArgs args)
            {
                _wait.SetResult(args);
            }
            /// <summary>
            /// Устанавливает факт ошибки
            /// </summary>
            /// <param name="exp">Произошедшая ошибка</param>
            public void SetException(Exception exp)
            {
                _wait.SetException(exp);
            }
        }



        /// <summary>
        /// Карта сериализации исходящих сообщений
        /// </summary>
        private readonly ConcurrentDictionary<string, MemoryStream> _responseMessageMapping =
            new ConcurrentDictionary<string, MemoryStream>();

        /// <summary>
        /// Карта объектов синхронизации
        /// </summary>
        private readonly ConcurrentDictionary<string, IWaitHandler> _waitHandlers = new ConcurrentDictionary<string, IWaitHandler>();

        private readonly IModel _channel;

        /// <summary>
        /// Определяет был ли объект завершён
        /// </summary>
        private volatile bool _isDispose;

        private readonly EventingBasicConsumer _consumer;
        private readonly string _clientProccessName;
        /// <summary>
        /// Имя очереди
        /// </summary>
        private string _queueName;
        /// <summary>
        /// Имя очереди, куда отправляются команды
        /// </summary>
        private string _commandQueueName;
        private string _consumerTag;
        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="BaseClient"/>.
        /// </summary>
        /// <param name="connection"> Соединение с RabbitMq </param>
        /// <param name="queueName">имя очереди</param>
        protected BaseClient(IConnection connection, string commandQueueName)
        {
            if (commandQueueName == null)
                throw new ArgumentNullException($"Argument {nameof(commandQueueName)} is null");

            _commandQueueName = commandQueueName;

            _clientProccessName = connection.ClientProvidedName ?? Process.GetCurrentProcess().ProcessName;
            _channel = connection.CreateModel();
            _channel.BasicReturn += ChannelBaseReturn;



            _queueName = "amq.rabbitmq.reply-to";
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += OnMessageRecieved;
            _consumerTag = _channel.BasicConsume(_queueName, true, _consumer);
        }
        protected BaseClient(IConnection connection)
            : this(connection, SharebleConst.RmqQueueName)
        {

        }



        /// <summary>
        /// Обработка события о не доставленных сообщениях
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Источник исключения</param>
        private void ChannelBaseReturn(object sender, BasicReturnEventArgs e)
        {
            IWaitHandler waitDesc;
            if (_waitHandlers.TryGetValue(e.BasicProperties.CorrelationId, out waitDesc))
            {
                waitDesc.SetException(new InvalidOperationException(string.Format("Message not be sent, reason : {0}", e.ReplyText)));
            }
        }

        /// <summary>
        /// Обработчик получения сообщения
        /// </summary>
        /// <param name="sender">Источник события</param>
        /// <param name="e">Параметр события</param>
        private void OnMessageRecieved(object sender, BasicDeliverEventArgs e)
        {
            IWaitHandler retVal;
            Guid corrId;
            if (e.BasicProperties != null &&
                _waitHandlers.TryGetValue(e.BasicProperties.CorrelationId, out retVal))
            {
                retVal.Set(e);
            }
        }

        ///// <summary>
        ///// Регистрирует объект ожидания
        ///// </summary>
        ///// <param name="corrId">Идентификатор процесса</param>
        ///// <param name="waitHandler">Объект ожидания</param>
        //protected void RegisterWaitHandler(Guid corrId, IWaitHandler waitHandler)
        //{
        //    _waitHandlers.GetOrAdd(corrId.ToString(), waitHandler);
        //}

        /// <summary>
        /// Снимает с регистрации объект ожидания
        /// </summary>
        /// <param name="corrId">идентификатор процесса</param>
        protected void UnregisterWaitHandler(Guid corrId)
        {
            IWaitHandler wait;
            _waitHandlers.TryRemove(corrId.ToString(), out wait);
        }
        /// <summary>
        /// Разбирает полученное сообщение
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="mess">Разбираемое сообщение</param>
        /// <returns>Результат разбора</returns>
        protected ResponseMessage ParseMessage(BasicDeliverEventArgs mess) /*where T:class*/
        {
            var dc = _responseMessageMapping.GetOrAdd(mess.BasicProperties.ContentType, FindDcByName);
            //var ms = new MemoryStream(mess.Body);
            string messageBodyString = Encoding.UTF8.GetString(mess.Body.ToArray());
            var readObject = JsonConvert.DeserializeObject<ResponseMessage>(messageBodyString); //ResponseMessage.Parser.ParseFrom(mess.Body); //dc.ReadObject(ms); 

            var error = readObject.ErrorResponse;
            if (error != null)
            {
                switch (error.ErrorType)
                {

                    default:
                        throw new InterlockManagerException(error.Error);
                }
            }
            return readObject as ResponseMessage;
        }

        /// <summary>
        /// Асинхронная ожидающая отправка сообщений
        /// </summary>
        /// <typeparam name="T">Тип принимаемого сообщения </typeparam>
        /// <param name="message"> Отправляемое сообщение </param>
        /// <param name="token">Токен для отмены операции</param>
        /// <returns> Полученный результат  </returns>
        protected async Task<ResponseMessage> AsyncWaitableSend(RequestMessage message, CancellationToken token) /*where T:class*/ /*where T : ResponseMessage*/
        {
            var waiter = new WaitDesc();

            message.Header = new Header();
            message.Header.CorrelationId = Guid.NewGuid().ToString();

            _waitHandlers.GetOrAdd(message.Header.CorrelationId, waiter);
            SendRequest(message);

            IWaitHandler remVal;
            try
            {
                var waitAsync = await waiter.WaitAsync(token);
                _waitHandlers.TryRemove(message.Header.CorrelationId, out remVal);
                return ParseMessage(waitAsync/*.Result*/);
            }
            finally
            {
                _waitHandlers.TryRemove(message.Header.CorrelationId, out remVal);
            }
        }

        /// <summary>
        /// отправка обратного сообщения 
        /// </summary>
        /// <param name="message">Отправляемое сообщение</param>
        protected void SendRequest(RequestMessage message)
        {
            lock (_channel)
            {
                try
                {
                    message.Header.ReplyTo = _queueName;
                    message.Header.AppName = _clientProccessName;

                    var prop = _channel.CreateBasicProperties();
                    prop.CorrelationId = message.Header.CorrelationId;
                    prop.ReplyTo = message.Header.ReplyTo;
                    prop.ContentType = message.GetType().Name;


                    var messageSearialized = JsonConvert.SerializeObject(message).ToCharArray();
                    byte[] bytes = Encoding.UTF8.GetBytes(messageSearialized);
                    var ms = new MemoryStream();

                    ms.Write(bytes, 0, bytes.Length);
                    var msArray = ms.ToArray();

                    var consCount = _channel.ConsumerCount(_queueName);
                    if (consCount == 0)
                    {
                        throw new InterlockManagerException("Interlock Manager not started");
                    }
                    _channel.ConfirmSelect();

                    _channel.BasicPublish("", _commandQueueName, true, prop, msArray);
                    _channel.WaitForConfirmsOrDie();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message} {ex.StackTrace}");
                }

            }
        }



        ///// <summary>
        ///// Находит DataContractSerialize по имени типа поиск происходит только среди определённых классов текущей сборки
        ///// </summary>
        ///// <param name="name">Имя типа</param>
        ///// <returns>Найденный сереализатор</returns>
        private MemoryStream FindDcByName(string name)
        {
            foreach (var type in typeof(ResponseMessage).Assembly.GetTypes().
                Where(o => /*o.BaseType == typeof(ResponseMessage) &&*/ o.Name == name))
            {
                return new MemoryStream(); //DataContractSerializer(type);
            }
            throw new InvalidDataException("Invalid message receive");
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            try
            {
                _channel.BasicCancel(_consumerTag);
                _channel.Dispose();
            }
            catch (Exception) { }
            _isDispose = true;
        }
    }
}
