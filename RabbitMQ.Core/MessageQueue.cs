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
    public partial class MessageQueue
    {
        public MessageQueue(string topic, string queueName = null, string routingKey = "")
        {
            Topic = topic;
            QueueName = queueName ?? topic;
            RoutingKey = routingKey;
        }
        public string QueueName { get; }
        public string Topic { get; }
        public string RoutingKey { get; }
    }
}