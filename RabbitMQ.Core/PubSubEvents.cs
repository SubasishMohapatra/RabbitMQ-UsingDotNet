using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Core
{
    public class QueueMaxSizeReachedPubSubEvent : PubSubEvent<(string oldQueueName,MessageQueue messageQueue)>
    {
    }
}
