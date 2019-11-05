using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class QueueListenerHostedServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public QueueListenerHostedServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public QueueListenerHostedService<T> Create<T>() where T : class
        {
            return new QueueListenerHostedService<T>(_serviceProvider.GetRequiredService<QueueServiceFactory>(),
                _serviceProvider.GetRequiredService<ILogger<QueueListenerHostedService<T>>>());
        }
    }
}