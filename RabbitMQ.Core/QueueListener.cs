using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Core
{
    /// <summary>
    /// Basic
    /// </summary>
    public partial class QueueListener : IDisposable
    {
        #region Fields

        IConnection _connection = null;
        private readonly int _noOfConsumers = 1;
        private bool _disposed = false;

        #endregion

        #region Constructors

        public QueueListener(string queueName, AsyncEventHandler<BasicDeliverEventArgs> callback)
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
            _connection = connectionFactory.CreateConnection();

            Channel = _connection.CreateModel();
            Channel.BasicQos(0, (ushort)(_noOfConsumers * 5), false);
            Channel.ContinuationTimeout = new TimeSpan(0, 30, 0);
            this.StartReceive(callback, queueName);
        }

        #endregion

        #region Properties

        public IModel Channel { get; private set; }

        #endregion

        /// <summary>
        /// Receiveds the specified action.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void StartReceive(AsyncEventHandler<BasicDeliverEventArgs> callback, string queueName)
        {
            for (int i = 1; i <= _noOfConsumers; i++)
            {
                AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(Channel);
                //The below "consumer tag" can be usd to cancel a consumer e.g., Channel.BasicCancel(consumerTag);
                var consumerTag = Channel.BasicConsume(queueName, false, consumer);
                consumer.Received += async (sender, eventArgs) =>
                {
                    //Console.Write($"Consumer{consumerTag}-");
                    await PacketReceiveHandler(callback, sender, eventArgs);
                };
            }
        }

        private async Task PacketReceiveHandler(AsyncEventHandler<BasicDeliverEventArgs> callback, object sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                await callback(sender, eventArgs);
                Channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                int count = 0;

                if (eventArgs.BasicProperties.Headers == null)
                    eventArgs.BasicProperties.Headers = new Dictionary<string, object>();

                if (eventArgs.BasicProperties.Headers.ContainsKey("x-redelivered-count"))
                    count = (int)eventArgs.BasicProperties.Headers["x-redelivered-count"];
                else
                    eventArgs.BasicProperties.Headers.Add("x-redelivered-count", 0);

                count++;
                eventArgs.BasicProperties.Headers["x-redelivered-count"] = count;

                Channel.BasicNack(eventArgs.DeliveryTag, false, count > 0);
                //Logger.Write($"RabbitMQ Consume Catch::{ex.Message}", System.Diagnostics.TraceEventType.Error.ToString());
            }
        }

        #region IDispose implementation

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

        #endregion
    }
}