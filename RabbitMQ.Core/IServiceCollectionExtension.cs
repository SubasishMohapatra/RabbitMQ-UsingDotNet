using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prism.Events;
using RabbitMQ.Client.Events;
using System;

namespace RabbitMQ.Core
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddCoreService(this IServiceCollection services)
        {
            services.AddSingleton<IEventAggregator>(new EventAggregator());
            services.AddTransient<Func<string, QueueDispatchManager>>(serviceProvider => (topic) =>
            {
                return new QueueDispatchManager(topic);
            });
            services.AddTransient<Func<string,AsyncEventHandler<BasicDeliverEventArgs>, QueueListenerManager>>(serviceProvider => (queue,callback) =>
            {
                return new QueueListenerManager(queue, callback);
            });
            services.AddTransient<Func<string, string, string, bool, QueueDispatcher>>(serviceProvider => (exchange, queueName, routingKey, isLazy) =>
            {
                return new QueueDispatcher(exchange, queueName, routingKey, isLazy);
            });
            services.AddTransient<Func<string, AsyncEventHandler<BasicDeliverEventArgs>, QueueListener>>(serviceProvider => (queueName, messageReceivedCallBack) =>
            {
                return new QueueListener(queueName, messageReceivedCallBack);
            });
            services.Add(new ServiceDescriptor(typeof(Module), typeof(Module),ServiceLifetime.Scoped));
            return services;
        }
    }
}
