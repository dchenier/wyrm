
using System;
using System.Threading.Tasks;
using Wyrm.Events.Builder;
using Wyrm.RabbitMq.Extentions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IWyrmBuilderExtensions
    {
        public static WyrmOptionsBuilder UseRabbitMq(
            this WyrmOptionsBuilder optionsBuilder,
            string host,
            int? port = null)
        {
            optionsBuilder.Services.AddSingleton<QueueListenerHostedServiceFactory>();
            optionsBuilder.Services.Configure<QueueServiceOptions>(queueOptions => 
            {
                if (!string.IsNullOrWhiteSpace(host))
                   queueOptions.ServiceBusHostName = host;
                if (port.HasValue)
                {
                    queueOptions.ServiceBusPort = port.Value;
                }
            });
            optionsBuilder.Services.AddSingleton<QueueServiceFactory>();
            optionsBuilder.Services.AddTransient<IQueueService, QueueService>();

            var extension = (RabbitMqWyrmOptionsExtension)GetOrCreateExtension(optionsBuilder).WithHost(host);
            ((IWyrmInfrastructureBuilder)optionsBuilder).AddOrUpdateExtension(extension);

            return optionsBuilder;
        }

        private static RabbitMqWyrmOptionsExtension GetOrCreateExtension(WyrmOptionsBuilder optionsBuilder)
            => optionsBuilder.Options.FindExtension<RabbitMqWyrmOptionsExtension>()
               ?? new RabbitMqWyrmOptionsExtension();        
    }
}