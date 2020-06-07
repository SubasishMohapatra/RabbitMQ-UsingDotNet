using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Prism.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;

namespace RabbitMQ.Core
{
    /// <summary>
    /// Basic
    /// </summary>
    public class QueueDispatcher : IDisposable
    {
        #region Fields

        private bool _disposed;
        string _queueName;
        private IBasicProperties _basicProperties;
        IConnection _connection = null;
        private readonly int _noOfConsumers = 1;
        private ManualResetEventSlim _manualResetEventSlim = new ManualResetEventSlim(true);
        MessageQueue messageQueue = null;

        #endregion Fields    

        #region Constructors

        public QueueDispatcher(string topic, string queueName = null, string routingKey = "", bool isLazy = true)
        {
            CreateQueue(topic, queueName, routingKey, isLazy);
        }

        private void CreateQueue(string topic, string queueName, string routingKey, bool isLazy)
        {
            var builder = new ConfigurationBuilder()
                  .AddJsonFile("AppSettings.json");
            var configuration = builder.Build().GetSection("RabbitMQ");

            var queueServer = configuration["QueueServer"];
            var queueUsername = configuration["QueueUsername"];
            var queuePassword = configuration["QueuePassword"];

            var connectionFactory = new ConnectionFactory { HostName = queueServer, UserName = queueUsername, Password = queuePassword, DispatchConsumersAsync = true };
            connectionFactory.AutomaticRecoveryEnabled = true;
            connectionFactory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            var exchange = $"exchange.{topic}";
            _connection = connectionFactory.CreateConnection();

            Channel = _connection.CreateModel();
            Channel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct, true);
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            if (isLazy)
                arguments.Add("x-queue-mode", "lazy");
            _queueName = queueName ?? topic;
            Channel.QueueDeclare(_queueName, true, false, false, arguments);
            Channel.BasicQos(0, (ushort)(_noOfConsumers * 5), false);
            Channel.QueueBind(queue: _queueName,
                              exchange: exchange,
                              routingKey: routingKey, arguments);

            Channel.ContinuationTimeout = new TimeSpan(0, 30, 0);
            _basicProperties = Channel.CreateBasicProperties();
            _basicProperties.Persistent = true;
            _basicProperties.DeliveryMode = 2;
            messageQueue = new MessageQueue(topic, queueName, routingKey);
        }

        #endregion Constructors

        public IModel Channel { get; private set; }
                
        #region Methods        

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public void Publish(object payload)
        {
            try
            {
                string message = JsonConvert.SerializeObject(payload);
                byte[] body = Encoding.UTF8.GetBytes(message);
                if (Channel.IsOpen)
                    Channel.BasicPublish("", _queueName, _basicProperties, body);
            }
            catch (Exception ex)
            {
                //Logger.Write(ex);
                throw;
            }
        }

        public void Publish(string payload, string topic, string routingKey = "")
        {
            try
            {
                byte[] body = Encoding.UTF8.GetBytes(payload);
                //MessageQueue messageQueue = null;
                _manualResetEventSlim.Wait();
                //Create new queue with prefix same as topic name
                if (Channel.MessageCount(_queueName) >= Consts.QueueMaxSize - 1)
                {
                    _manualResetEventSlim.Reset();
                    this.Reset(out messageQueue);
                    ////to reroute pending messages which are waiting 
                    routingKey = messageQueue.RoutingKey;
                    if (Channel.IsOpen)
                        Channel.BasicPublish($"exchange.{topic}", routingKey, _basicProperties, body);
                    _manualResetEventSlim.Set();
                    return;
                }
                //to reroute pending messages which are waiting 
                routingKey = (messageQueue?.RoutingKey) ?? routingKey;
                var exchange = $"exchange.{topic}";
                if (Channel.IsOpen)
                    Channel.BasicPublish(exchange, routingKey, _basicProperties, body);
            }
            catch (Exception ex)
            {
                //Logger.Write(ex);
                throw;
            }
        }

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="payload">The payload.</param>
        public void Publish(object payload, string topic, string routingKey = "")
        {
            try
            {
                string message = JsonConvert.SerializeObject(payload);
                byte[] body = Encoding.UTF8.GetBytes(message);
                var exchange = $"exchange.{topic}";
                if (Channel.IsOpen)
                    Channel.BasicPublish(exchange, routingKey, _basicProperties, body);
            }
            catch (Exception ex)
            {
                //Logger.Write(ex);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="headerValues"></param>
        public void Publish(object payload, Dictionary<string, object> headerValues)
        {
            try
            {
                string message = JsonConvert.SerializeObject(payload);
                byte[] body = Encoding.UTF8.GetBytes(message);
                _basicProperties.Headers = headerValues;
                if (Channel.IsOpen)
                    Channel.BasicPublish("", _queueName, _basicProperties, body);

                _basicProperties.Type = string.Empty;

            }
            catch (Exception ex)
            {
                //Logger.Write(ex);
                throw;
            }
        }

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Publish(string message)
        {
            byte[] body = Encoding.UTF8.GetBytes(message);
            Channel.BasicPublish("", _queueName, _basicProperties, body);
        }

        public void Clear()
        {
            Channel?.Close();
            _connection?.Close();
            Channel?.Dispose();
            _connection?.Dispose();
            Channel = null;
            _connection = null;
        }

        #region Private methods

        private void Reset(out MessageQueue messageQueue)
        {
            this.Clear();
            var oldQueueName = _queueName;
            //Get new temporary queueName
            messageQueue = GenerateTemporaryQueueName();
            this.CreateQueue(messageQueue.Topic, messageQueue.QueueName, messageQueue.RoutingKey, true);
            var serviceProvider = Module.GetServiceProvider();
            var eventAggregator = serviceProvider.GetService<IEventAggregator>();
            eventAggregator.GetEvent<QueueMaxSizeReachedPubSubEvent>().Publish((oldQueueName, messageQueue));
        }

        private MessageQueue GenerateTemporaryQueueName()
        {
            if (_queueName.StartsWith("tmp"))
            {
                var splitResult = _queueName.Split('.');
                //Parse and increment the routing key
                var inc = int.Parse(splitResult[0].Substring(3)) + 1;
                var topic = splitResult[1];
                var routingKey = inc.ToString();
                return new MessageQueue(topic, $"tmp{routingKey}.{topic}", routingKey);
            }
            return new MessageQueue(_queueName, $"tmp1.{_queueName}", "1");
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Channel?.Close();
                    _connection?.Close();
                    Channel?.Dispose();
                    _connection?.Dispose();
                    Channel = null;
                    _connection = null;
                }
                _disposed = true;
            }
        }

        #endregion Methods
    }
}