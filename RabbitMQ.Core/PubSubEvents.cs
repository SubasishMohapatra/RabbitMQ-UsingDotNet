using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Core
{
    public class MaxQueueSizeReachedPubSubEvent : PubSubEvent<(string queueName, string payload)>
    {
    }

    public class MinQueueSizeReachedPubSubEvent : PubSubEvent<string>
    {
    }
    
}
