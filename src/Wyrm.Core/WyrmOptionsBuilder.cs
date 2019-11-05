using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wyrm.Events.Hosting;

namespace Wyrm.Events.Builder
{
    public class WyrmOptionsBuilder : IWyrmInfrastructureBuilder
    {
        private WyrmOptions _options;
        private readonly IServiceCollection _serviceCollection;

        public WyrmOptionsBuilder(IServiceCollection services) : this(services, new WyrmOptions()) {}
        public WyrmOptionsBuilder(IServiceCollection services, WyrmOptions options) 
        {
            _serviceCollection = services ?? throw new ArgumentNullException(nameof(services));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private void AddService<TService>(
            ServiceLifetime serviceLifetime,
#if NETSTANDARD2_0
            Func<IServiceProvider, TService> implementationFactory = null)
#else
            Func<IServiceProvider, TService>? implementationFactory = null)
#endif            
            where TService : class
        {
            // I bet there an easier way to do this...!

            if (serviceLifetime == ServiceLifetime.Singleton)
            {
                if (implementationFactory == null)
                    _serviceCollection.AddSingleton<TService>();
                else
                    _serviceCollection.AddSingleton<TService>(implementationFactory);
            }
            else if (serviceLifetime == ServiceLifetime.Transient)
            {
                if (implementationFactory == null)
                    _serviceCollection.AddTransient<TService>();
                else
                    _serviceCollection.AddTransient<TService>(implementationFactory);
            }
            else
            {
                if (implementationFactory == null)
                    _serviceCollection.AddScoped<TService>();
                else
                    _serviceCollection.AddScoped<TService>(implementationFactory);
            }

        }

        public WyrmOptionsBuilder AddEventHandler<TEventHandler>(
#if NETSTANDARD2_0
            Action<EventHandlerOptions> options = null,
            Func<IServiceProvider, TEventHandler> implementationFactory = null,
#else
            Action<EventHandlerOptions>? options = null,
            Func<IServiceProvider, TEventHandler>? implementationFactory = null,
#endif            
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
            where TEventHandler: class, IEventHandler
        {
            AddService<TEventHandler>(serviceLifetime, implementationFactory);
 
            // add service host for the event handler
            _serviceCollection.AddHostedService<EventHandlerHostedService<TEventHandler>>(services => 
            {
                var logger = services.GetRequiredService<ILogger<WyrmOptionsBuilder>>();
                logger.LogInformation("Creating hosted service for type " + typeof(TEventHandler).FullName);

                var eventBuilder = services.GetRequiredService<IEventBuilder>();
                var eventHandlerOptions = new EventHandlerOptions(eventBuilder);
                if (options != null)
                {
                    options(eventHandlerOptions);
                }

                _options.WithEventHandler<TEventHandler>(services, eventBuilder);
                var eventDelegate = eventBuilder.Build();
                if (eventHandlerOptions.InstanceCount <= 1)
                {
                    return new EventHandlerHostedService<TEventHandler>(
                        _options.CreateEventService<TEventHandler>(services),
                        eventDelegate);
                }
                else 
                {
                    var eventServices = new List<IEventService>();
                    for (int i=0; i<eventHandlerOptions.InstanceCount; i++) 
                    {
                        eventServices.Add(_options.CreateEventService<TEventHandler>(services));
                    }
                    return new EventHandlerHostedService<TEventHandler>(eventServices, eventDelegate);
                }
            });

            return this;
        }


        public IServiceCollection Services => _serviceCollection;
        public virtual WyrmOptions Options => _options;

        void IWyrmInfrastructureBuilder.AddOrUpdateExtension<TExtension>(TExtension extension)
        {
            _options = Options.WithExtension(extension);
        }
    }
}