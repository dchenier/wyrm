using System;
using Wyrm.Events.Hosting;

namespace Wyrm.Events.Builder
{
    public interface IWyrmOptionsExtension
    {
        void AddEventHandler<TEventHandler>(IServiceProvider services, IEventBuilder eventBuilder)
            where TEventHandler : class, IEventHandler;

        IEventService CreateEventService<TEventHandler>(IServiceProvider services)
            where TEventHandler : class, IEventHandler;
    }
}