using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RabbitMQ.Core;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddCoreService();
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<Module>();
            var queueListenerManager = serviceProvider.GetService<Func<string, AsyncEventHandler<BasicDeliverEventArgs>, QueueListenerManager>>()("Test", OnReceivePacket);
            queueListenerManager.Initialize();
            Console.ReadLine();
        }

        /// <summary>
        /// Handle packet receive event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public static async Task OnReceivePacket(object sender, BasicDeliverEventArgs eventArgs)
        {
            //Each . represents a wait time of 200ms
            string message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            Console.WriteLine(message);
            var count = message.Sum(x => (x == '.' ? 1 : 0));
            //await Task.Delay((count == 0 ? 1 : count) * 200);
        }
    }
}
