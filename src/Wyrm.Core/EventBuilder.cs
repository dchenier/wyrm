using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wyrm.Events.Builder
{
    //
    // Summary:
    //     Defines a contract for an event builder in an application. An event builder specifies
    //     the events for an application.
    internal class EventBuilder : IEventBuilder
    {
        private readonly IList<Func<EventDelegate, EventDelegate>> _components = 
            new List<Func<EventDelegate, EventDelegate>>();

        private readonly ILogger<EventBuilder> _logger;

        public EventBuilder(IServiceProvider serviceProvider,
            ILogger<EventBuilder> logger) { 
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        //
        // Summary:
        //     Gets the sets the System.IServiceProvider used to resolve services for events.
        public IServiceProvider ServiceProvider { get; }

        public IEventBuilder Use(Func<EventDelegate, EventDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }


        public EventDelegate Build()
        {
            _logger.LogDebug("Building EventDelegate");

            //(similar to https://github.com/aspnet/AspNetCore/blob/4ef204e13b88c0734e0e94a1cc4c0ef05f40849e/src/Http/Http/src/Builder/ApplicationBuilder.cs#L82)
            EventDelegate app = context =>
            {
                // last middleware to run is a no-op
                return Task.CompletedTask;
            };
            
            foreach (var component in _components.Reverse())
            {
                app = component(app);
            }

            return app;
        }       
    }
}