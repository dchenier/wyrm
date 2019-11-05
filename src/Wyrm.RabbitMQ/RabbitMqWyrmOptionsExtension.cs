using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wyrm.Events;
using Wyrm.Events.Builder;
using Wyrm.Events.Hosting;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class RabbitMqWyrmOptionsExtension : IWyrmOptionsExtension
    {
        private string _host = "localhost";

        public RabbitMqWyrmOptionsExtension() { }

        protected RabbitMqWyrmOptionsExtension(RabbitMqWyrmOptionsExtension copyFrom)
        {
            if (copyFrom == null)
                throw new ArgumentNullException(nameof(copyFrom));

            _host = copyFrom._host;
        }

        public RabbitMqWyrmOptionsExtension WithHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));

            var clone = new RabbitMqWyrmOptionsExtension(this)
            {
                _host = host
            };
            return clone;
        }

        public void AddEventHandler<TEventHandler>(IServiceProvider services, IEventBuilder eventBuilder)
            where TEventHandler : class, IEventHandler
        {

            var middleware = new EventHandlerMiddleware<TEventHandler>(services);
            eventBuilder.Use((context, next) => middleware.Handle(context, next));

            // add the processResultMiddleware if and only if there an EventAttribute with an out direction
            var eventAttributes = typeof(TEventHandler).GetCustomAttributes(typeof(EventAttribute), true)
                .OfType<EventAttribute>()
                .Where(e => e.Direction == Direction.Out)
                .ToList();

            if (eventAttributes.Count > 2)
            {
                throw new NotSupportedException("Type " + typeof(TEventHandler).FullName + ": Only one EventAttribute where Direction.Out is supported");
            }
            else if (eventAttributes.Count == 1)
            {
                var eventAttribute = eventAttributes[0];

                var nextMiddleWare = new ProcessResultMiddleware(services, eventAttributes[0]);
                eventBuilder.Use((context, next) => nextMiddleWare.Handle(context, next));
            }
        }

        public IEventService CreateEventService<TEventHandler>(IServiceProvider services)
            where TEventHandler : class, IEventHandler
        {
            var factory = services.GetRequiredService<QueueListenerHostedServiceFactory>();
            var service = factory.Create<TEventHandler>();
            return SetQueueCreationStrategy<TEventHandler>(service);
        }

        private QueueListenerHostedService<TEventHandler> SetQueueCreationStrategy<TEventHandler>(
            QueueListenerHostedService<TEventHandler> service)
            where TEventHandler : class
        {
            // look for an EventAttribute, and use the event name for the queue name
            var eventAttributes = typeof(TEventHandler).GetCustomAttributes(typeof(EventAttribute), true)
                .OfType<EventAttribute>()
                .Where(e => e.Direction == Direction.In)
                .ToList();

            if (eventAttributes.Count > 1)
            {
                throw new NotSupportedException("Type " + typeof(TEventHandler).FullName + ": Only one EventAttribute on Direction.In is supported");
            }
            else if (eventAttributes.Count == 0)
            {
                throw new NotSupportedException("Type " + typeof(TEventHandler).FullName + ": Currently EventHandlers need an EventAttribute. The Event Name will be used for the queue name");
            }

            var eventAttribute = eventAttributes[0];
            if (string.IsNullOrWhiteSpace(eventAttribute.EventName))
            {
                throw new ArgumentException("Type " + typeof(TEventHandler).FullName + ": EventAttribute.EventName cannot be null", nameof(EventAttribute.EventName));
            }

            if (eventAttribute.AllowMultipleConsumers)
            {
                service.SetFanoutExchange(eventAttribute.EventName);
            }
            else
            {
                service.SetQueueName(eventAttribute.EventName);
            }

            return service;
        }


    }
}