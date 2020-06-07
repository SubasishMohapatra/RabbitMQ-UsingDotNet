using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prism.Events;

namespace RabbitMQ.Core
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddCoreService(this IServiceCollection services)
        {
            services.AddSingleton<IEventAggregator>(new EventAggregator());
            services.AddSingleton<QueueOrchestrator>();
            services.Add(new ServiceDescriptor(typeof(Module), typeof(Module),ServiceLifetime.Scoped));
            return services;
        }
    }
}
