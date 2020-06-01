using System;
using RabbitMQ.Core;

namespace RabbitMQ.Sender
{
    class Program
    {
        static void Main(string[] args)
        {
            var queueDispatcher = new QueueDispatcher("Test");
            for (int i = 1; i <= 200; i++)
            {
                var count = i % 4;
                queueDispatcher.Publish($"Hello {i}"+ "".PadRight(count == 0 ? 1 : count,'.'));
            }
            Console.ReadLine();
        }

    }
}
