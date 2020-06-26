using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Prism.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Core
{
    /// <summary>
    /// Basic
    /// </summary>
    public partial class QueueListenerManager : IDisposable
    {
        #region Field declaration

        private SubscriptionToken _subscriptionToken = null;
        private QueueListener _queueListener = null;
        private HttpClient _httpClient = null;
        private string _virtualHost;
        private AsyncEventHandler<BasicDeliverEventArgs> _callback;
        //Main queueName
        private string _queueName;
        private SynchronizationContext _synchronizationContext;

        #endregion

        public QueueListenerManager(string queueName, AsyncEventHandler<BasicDeliverEventArgs> callback)
        {
            var serviceProvider = Module.GetServiceProvider();
            var eventAggregator = serviceProvider.GetService<IEventAggregator>();
            _callback = callback;
            _queueName = queueName;
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _subscriptionToken = eventAggregator.GetEvent<MinQueueSizeReachedPubSubEvent>().Subscribe((queue) =>
            {
                _synchronizationContext.Post(MinQueueSizeReached, queue);
            }, ThreadOption.BackgroundThread);
        }

        public void Initialize()
        {
            var username = "guest";
            var password = "guest";
            var host = "localhost";
            _virtualHost = Uri.EscapeDataString("/"); // %2F is the name of the default virtual host

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(string.Format(CultureInfo.InvariantCulture, "http://{0}:15672/", host))
            };
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var serviceProvider = Module.GetServiceProvider();
            _queueListener = serviceProvider.GetService<Func<string, AsyncEventHandler<BasicDeliverEventArgs>, QueueListener>>()(_queueName, _callback);
            if (CheckIfQueueEmpty())
            {
                MinQueueSizeReached(_queueName);
            }
        }

        private bool CheckIfQueueEmpty(string queueName = null)
        {
            queueName = queueName ?? _queueName;
            return (_queueListener?.Channel.MessageCount(queueName) == 0);
        }

        private void MinQueueSizeReached(object state)
        {
            string queueName = (string)state;
            var serviceProvider = Module.GetServiceProvider();
            if (string.Compare(queueName, _queueName, true) != 0)
            {
                _queueListener.Clear();
                Utility.DeleteQueues(_httpClient, _virtualHost, new string[] { queueName }).Wait();
            }
            var queues = Utility.GetQueues(_httpClient, _virtualHost).Result.Where(x => x.Name.EndsWith($".{_queueName}")).OrderBy(x => x.Name);
            //while ((queues?.Any() ?? false))
            //{
            //    if (string.Compare(queues.First().Name, queueName, true) == 0)
            //    {
            //        queues = Utility.GetQueues(_httpClient, _virtualHost).Where(x => x.Name.EndsWith($".{_queueName}")).OrderBy(x => x.Name);
            //    }
            //    else
            //        break;
            //}
            if ((queues?.Any() ?? false))
            {
                var newQueueName = queues.First().Name;
                _queueListener = serviceProvider.GetService<Func<string, AsyncEventHandler<BasicDeliverEventArgs>, QueueListener>>()(newQueueName, _callback);
                if (CheckIfQueueEmpty(newQueueName))
                {
                    MinQueueSizeReached(newQueueName);
                }
            }
            else
            {
                if (string.Compare(queueName, _queueName, true) != 0)
                    _queueListener = serviceProvider.GetService<Func<string, AsyncEventHandler<BasicDeliverEventArgs>, QueueListener>>()(_queueName, _callback);
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
                }
                _disposed = true;
            }
        }

        #endregion
    }
}