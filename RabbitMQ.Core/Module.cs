using Microsoft.Extensions.DependencyInjection;
using Prism.Events;
using System;

namespace RabbitMQ.Core
{
    public class Module
    {
        static IServiceProvider _serviceProvider;

        public Module(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private static readonly Lazy<IServiceProvider> ServiceProvider = new Lazy<IServiceProvider>(() =>
        {
            return _serviceProvider;
        });

        /// <summary>
        /// Gets the configured Unity container.
        /// </summary>
        internal static IServiceProvider GetServiceProvider()
        {
            return ServiceProvider.Value;
        }
    }
}
