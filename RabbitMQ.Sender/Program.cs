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
            var queueOrchestrator= serviceProvider.GetRequiredService<QueueOrchestrator>();
            //Deliberately we send ... after hello index,w ith each dot represntating a wait time of 200 ms
            for (int i = 1; i <= 200; i++)
            {
                var count = i % 4;
                queueOrchestrator.Publish($"Hello {i}"+ "".PadRight(count == 0 ? 1 : count,'.'),"Test");
            }
            Console.ReadLine();
        }

    }
}
