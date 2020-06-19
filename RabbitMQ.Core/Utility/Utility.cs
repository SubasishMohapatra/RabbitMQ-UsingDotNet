using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RabbitMQ.Core
{
    /// <summary>
    /// Basic
    /// </summary>
    public partial class Utility
    {
        public static IEnumerable<Task<HttpResponseMessage>> DeleteQueues(HttpClient httpClient, string virtualHost, string[] queueNames)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var queueDeletionTasks = new List<Task<HttpResponseMessage>>(queueNames.Length);
            queueDeletionTasks.AddRange(queueNames.Select(queueName =>
            {
                var deletionTask = httpClient.DeleteAsync(string.Format(CultureInfo.InvariantCulture, "/api/queues/{0}/{1}", virtualHost, queueName));
                deletionTask.ContinueWith((t, o) => { Console.WriteLine("Deleted queue {0}.", queueName); }, null, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
                deletionTask.ContinueWith((t, o) => { Console.WriteLine("Failed to delete queue {0}.", queueName); }, null, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
                return deletionTask;
            }));
            return queueDeletionTasks;
        }

        public static List<Queue> GetQueues(HttpClient httpClient, string virtualHost)
        {
            var queueResult = httpClient.GetAsync(string.Format(CultureInfo.InvariantCulture, "api/queues/{0}", virtualHost)).Result;
            queueResult.EnsureSuccessStatusCode();
            return JsonConvert.DeserializeObject<List<Queue>>(queueResult.Content.ReadAsStringAsync().Result);
        }

    }
    public class Queue
    {
        public string Name { get; set; }
    }
}