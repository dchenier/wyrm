using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wyrm.Events
{
    public abstract class EventHandler<TInput> : IEventHandler<TInput>
        where TInput : class

    {
        //public EventHandler() { Context = new EmptyContext();  }
        public EventHandler(EventContext context) { Context = context; }

        public EventContext Context { get; set; }

        public abstract Task HandleAsync(TInput payload, CancellationToken token);  
    }


    public abstract class EventHandler<TInput, TOutput> : IEventHandler<TInput, TOutput>
        where TInput : class
    {
        public EventHandler() { Context = new EmptyContext(); }

        public EventHandler(EventContext context) { Context = context; }

        public EventContext Context { get; set; }

        public abstract Task<TOutput> HandleAsync(TInput payload, CancellationToken token);
    }

    internal class EmptyContext : EventContext
    {
#if NETSTANDARD2_0
        public override IDictionary<string, object> Headers => null;
#else
        public override IDictionary<string, object>? Headers => null;
#endif   
    }

}