using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wyrm.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Wyrm.Events.Hosting;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class QueueListenerHostedService<T> : IEventService where T : class
    {
        private readonly IQueueService _queueService;
        private readonly ILogger<QueueListenerHostedService<T>> _logger;

        public QueueListenerHostedService(
            QueueServiceFactory queueServiceFactory,
            ILogger<QueueListenerHostedService<T>> logger)
        {
            if (queueServiceFactory == null)
                throw new ArgumentNullException(nameof(queueServiceFactory));
            _queueService = queueServiceFactory.CreateQueueService();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private string _queueName = string.Empty;

        public void SetQueueName(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentNullException(nameof(queueName));
            _queueService.EnsureQueue(queueName);
            _queueName = queueName;
        }

        public void SetFanoutExchange(string exchange)
        {
            _queueName = _queueService.CreateExchangeQueue(exchange);
        }

        public Task StartAsync(Func<EventContext, Task> onEventAsync, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_0
            string eventServiceName = typeof(T).FullName;
#else
            string? eventServiceName = typeof(T).FullName;
#endif   
            _logger.LogInformation("Starting QueueListenerHostedService for service {EventService}...", eventServiceName);
            _queueService.ReceiveMessages(_queueName, async ea =>
            {
                try
                {
                    _logger.LogDebug("{EventSerivce}: Received message ", eventServiceName);
                    await onEventAsync(new RabbitMessageContext(ea));
                }
                catch(Exception e)
                {
                    _logger.LogError(e, "Error in message handler: {ErrorMessage}", e.Message);
                }
                finally
                {

                }
            }, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // note that any currently executing tasks should be cancelled because
            // we passed in the cancellationToken to each "onReceivedeTask"s
            _queueService.StopReceivingMessages();
            return Task.CompletedTask;
        }


    }
}