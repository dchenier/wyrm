
using System.Threading;
using System.Threading.Tasks;

namespace Wyrm.Events
{
    /// <summary>
    /// interface specifies that this will handle events
    /// </summary>
    public interface IEventHandler 
    {
        EventContext Context { get; set; }
    }

    public interface IEventHandler<TPayload> : IEventHandler
        where TPayload : class
    {
        Task HandleAsync(TPayload payload, CancellationToken token);
    }

    public interface IEventHandler<TPayload, TOutput> : IEventHandler
        where TPayload : class
    {
        Task<TOutput> HandleAsync(TPayload payload, CancellationToken token);
    }
}