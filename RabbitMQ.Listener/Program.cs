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
            var queueListener = new QueueListener("Test", OnReceivePacket);
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
            string message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            Console.WriteLine(message);
            var count = message.Sum(x => (x == '.' ? 1 : 0));
            await Task.Delay((count == 0 ? 1 : count) * 200);
        }
    }
}
