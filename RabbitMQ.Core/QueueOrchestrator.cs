using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Prism.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
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
        ConcurrentDictionary<string, string> _linkedQueues = new ConcurrentDictionary<string, string>();
        ConcurrentQueue<string> _bufferedMessages = new ConcurrentQueue<string>();
        SubscriptionToken _subscriptionToken = null;
        #endregion

        public QueueOrchestrator()
        {
            var serviceProvider = Module.GetServiceProvider();
            var eventAggregator = serviceProvider.GetService<IEventAggregator>();
            _subscriptionToken=eventAggregator.GetEvent<MaxQueueSizeReachedPubSubEvent>().Subscribe((x) => MaxQueueSizeReached(x.queueName, x.payload));
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

        private void MaxQueueSizeReached(string queueName, string payload)
        {
            _manualResetEventSlim.Reset();
            if (_linkedQueues.ContainsKey(queueName) == false)
            {
                _bufferedMessages.Enqueue(payload);
                _linkedQueues.TryAdd(queueName, "");
                var topic = queueName.StartsWith(Consts.TemporaryQueuePrefix) ? queueName.Split('.')[1] : queueName;
                _queueDispatcher.Publish(Consts.ClearQueueMessage, topic);
            }
            else
            {
                if (payload == Consts.ClearQueueMessage)
                {
                    _queueDispatcher.Clear();
                    var oldQueueName = queueName;
                    //Get new temporary queueName
                    _messageQueue = GenerateTemporaryQueueName(queueName);
                    _queueDispatcher = new QueueDispatcher(_messageQueue.Topic, _messageQueue.QueueName, _messageQueue.RoutingKey, true);
                    //Map oldName with new name(old is key, new is value)
                    _linkedQueues[queueName] = _messageQueue.QueueName;
                    while (_bufferedMessages.Count > 0)
                    {
                        _bufferedMessages.TryDequeue(out string message);
                        _queueDispatcher.Publish(message, _messageQueue.Topic, _messageQueue.RoutingKey);
                    }
                    _bufferedMessages = new ConcurrentQueue<string>();
                    _manualResetEventSlim.Set();
                    return;
                }
                _bufferedMessages.Enqueue(payload);
            }
        }

        private MessageQueue GenerateTemporaryQueueName(string queueName)
        {
            if (queueName.StartsWith(Consts.TemporaryQueuePrefix))
            {
                var splitResult = queueName.Split('.');
                //Parse and increment the routing key
                var inc = int.Parse(splitResult[0].Substring(3)) + 1;
                var topic = splitResult[1];
                var routingKey = inc.ToString();
                return new MessageQueue(topic, $"{Consts.TemporaryQueuePrefix}{routingKey}.{topic}", routingKey);
            }
            return new MessageQueue(queueName, $"{Consts.TemporaryQueuePrefix}1.{queueName}", "1");
        }

        public void Publish(string payload, string topic)
        {
            _manualResetEventSlim.Wait();
            //Get appropriate messageQueue fom the parameter topic and get current routingKey

            try
            {
                _queueDispatcher.Publish(payload, topic, _messageQueue.RoutingKey);
            }
            catch (Exception ex)
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
                    _subscriptionToken.Dispose();
                    _subscriptionToken = null;
                    _queueDispatcher?.Dispose();
                    _queueDispatcher = null;
                }
                _disposed = true;
            }
        }

        #endregion
    }
}