using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Wyrm.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Wyrm.Events.Builder;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class ProcessResultMiddleware
    {
        //private readonly IServiceProvider _serviceProvider;
        private readonly IQueueService _queueService;
        private readonly ILogger<ProcessResultMiddleware> _logger;
        private readonly bool _isExchange;
#if NETSTANDARD2_0
        private readonly string _queueName;
        private readonly string _exchangeName;
#else
        private readonly string? _queueName;
        private readonly string? _exchangeName;
#endif

        public ProcessResultMiddleware(IServiceProvider serviceProvider,
            EventAttribute outEvent)
        {
            //_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _queueService = serviceProvider.GetRequiredService<IQueueService>();

            // we'll allow the logger to be null
            _logger = serviceProvider.GetService<ILogger<ProcessResultMiddleware>>();


            // ensure the queue exists
            if (outEvent.AllowMultipleConsumers)
            {
                _isExchange = true;
                _exchangeName = outEvent.EventName;
                _queueService.EnsureExchange(outEvent.EventName);
            }
            else
            {
                _isExchange = false;
                _queueName = outEvent.EventName;
                _queueService.EnsureQueue(outEvent.EventName);
            }
        }


        private void LogError(Exception error, string message, params object[] args)
        {
            if (_logger != null)
            {
                _logger.LogError(error, message, args);
            }
        }

        public async Task Handle(EventContext context, Func<Task> next)
        {
            var rabbitContext = context as RabbitMessageContext;
            if (rabbitContext?.Result != null)
            {
                try
                {
                    if (_isExchange)
                    {
                        if (string.IsNullOrWhiteSpace(_exchangeName))
                        {
                            throw new InvalidOperationException("Exchange name is not set");
                        }

                        // we will pass only the same headers as we got in.
                        _queueService.SendExchangeMessage(exchangeName: _exchangeName,
                            payload: rabbitContext.Result,
                            options =>
                        {
                            if (rabbitContext.Headers != null)
                            {
                                foreach (var key in rabbitContext.Headers.Keys)
                                {
                                    if (!options.Headers.ContainsKey(key))
                                        options.Headers[key] = rabbitContext.Headers[key];
                                }
                            }
                        });
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(_queueName))
                        {
                            throw new InvalidOperationException("Queue name is not set");
                        }

                        // we will pass only the same headers as we got in.
                        _queueService.SendMessage(queueName: _queueName,
                            payload: rabbitContext.Result,
                            options =>
                        {
                            if (rabbitContext.Headers != null)
                            {
                                foreach (var key in rabbitContext.Headers.Keys)
                                {
                                    if (!options.Headers.ContainsKey(key))
                                        options.Headers[key] = rabbitContext.Headers[key];
                                }
                            }
                        });
                    }
                }
                catch(Exception err)
                {
                    LogError(err, "Error Processing EventHandler result: {ErrorMessage}", err.Message);
                    throw;
                }
            }
            if (next != null)
                await next();
        }
    }
}