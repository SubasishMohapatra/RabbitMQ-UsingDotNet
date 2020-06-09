using Prism.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Core
{
    public class Consts 
    {
        public const int QueueMaxSize = 50;

        public const string ClearQueueMessage = "Flush";

        public const string TemporaryQueuePrefix = "tmp";
    }
}
