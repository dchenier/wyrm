using System;

namespace Wyrm.Events.Builder
{
    //
    // Summary:
    //     Defines a contract for an event builder in an application. An event builder specifies
    //     the events for an application.
    public interface IEventBuilder
    {
        IEventBuilder Use(Func<EventDelegate, EventDelegate> middleware);

        EventDelegate Build();
    }
}