using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Prism.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Core
{
    /// <summary>
    /// Basic
    /// </summary>
    public partial class QueueOrchestrator : IDisposable
    {
        #region Field declaration

        QueueDispatcher _queueDispatcher;
        MessageQueue _messageQueue = null;
        ManualResetEventSlim _manualResetEventSlim = new ManualResetEventSlim(true);
        #endregion

        public QueueOrchestrator()
        {
            var serviceProvider = Module.GetServiceProvider();
            var eventAggregator = serviceProvider.GetService<IEventAggregator>();
            eventAggregator.GetEvent<QueueMaxSizeReachedPubSubEvent>().Subscribe((x) => QueueFull_Handler(x.oldQueueName, x.messageQueue), ThreadOption.BackgroundThread);
            _messageQueue = new MessageQueue("Test");
            _queueDispatcher = new QueueDispatcher("Test");
        }

        private void QueueFull_Handler(string oldQueueName, MessageQueue messageQueue)
        {
            _manualResetEventSlim.Reset();
            //Reroute messages using new routing key received
            _messageQueue = messageQueue;
            _manualResetEventSlim.Set();
        }

        public void Publish(string payload, string topic)
        {
            _manualResetEventSlim.Wait();
            //Get appropriate messageQueue fom the parameter topic and get current routingKey

            try
            {
                _queueDispatcher.Publish(payload, topic, _messageQueue.RoutingKey);
            }
            catch(Exception ex)
            {
                _manualResetEventSlim.Wait();
                _queueDispatcher.Publish(payload, topic, _messageQueue.RoutingKey);
            }

        }

        #region IDispose implementation

        bool _disposed = false;

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
                    _queueDispatcher?.Dispose();
                    _queueDispatcher = null;
                }
                _disposed = true;
            }
        }

        #endregion
    }
}