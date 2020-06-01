using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

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

        #endregion Fields    

        #region Constructors

        public QueueDispatcher(string topic, string routingKey = "", bool isLazy = true)
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
            _queueName = Channel.QueueDeclare(topic, true, false, false, arguments).QueueName;
            Channel.BasicQos(0, (ushort)(_noOfConsumers*5),false);
            Channel.QueueBind(queue: _queueName,
                              exchange: exchange,
                              routingKey: routingKey,arguments);

            Channel.ContinuationTimeout = new TimeSpan(0, 30, 0);
            _basicProperties = Channel.CreateBasicProperties();
            _basicProperties.Persistent = true;
            _basicProperties.DeliveryMode = 2;
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