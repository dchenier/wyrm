using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class QueueServiceFactory 
    {
        private readonly IServiceProvider _serviceProvider;
        public QueueServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        public IQueueService CreateQueueService()
        {
            return _serviceProvider.GetRequiredService<IQueueService>();
            //return new QueueService(_serviceProvider.GetRequiredService<IOptions<QueueServiceOptions>>(),
            //    _serviceProvider.GetRequiredService<ILogger<QueueService>>());
        }
    }
}