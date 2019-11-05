using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wyrm.Events.Hosting
{
    public interface IEventService
    {
         Task StartAsync(Func<EventContext, Task> onEventAsync, CancellationToken cancellationToken);
         Task StopAsync(CancellationToken cancellationToken);
    }
}