using System;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Core;

namespace RabbitMQ.Sender
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddCoreService();
            //services.AddSingleton<QueueOrchestrator>(new QueueOrchestrator());
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<Module>();
            Console.WriteLine("Queues and exchanges creation started...");
            var queueDispatchManager = serviceProvider.GetRequiredService<Func<string, QueueDispatchManager>>()("Test");
            //Deliberately we send ... after hello index,w ith each dot represntating a wait time of 200 ms
            for (int i = 1; i <= 200; i++)
            {
                var count = i % 4;
                queueDispatchManager.Publish($"Hello {i}"+ "".PadRight(count == 0 ? 1 : count,'.'),"Test");
            }
            Console.WriteLine("Queues and exchanges created.");
            Console.ReadLine();
        }

    }
}
