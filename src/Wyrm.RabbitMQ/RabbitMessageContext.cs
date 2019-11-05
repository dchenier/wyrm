using System.Collections.Generic;
using System.Security.Claims;
using Wyrm.Events;
using RabbitMQ.Client.Events;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class RabbitMessageContext : EventContext
    {
        public RabbitMessageContext(BasicDeliverEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }

        public BasicDeliverEventArgs EventArgs { get; }

#if NETSTANDARD2_0
        public override IDictionary<string, object> Headers => EventArgs.BasicProperties.IsHeadersPresent() ? EventArgs.BasicProperties.Headers : null;

        public override ClaimsPrincipal User { get; set; }
        internal object Result { get; set; }
#else
        public override IDictionary<string, object>? Headers => EventArgs.BasicProperties.IsHeadersPresent() ? EventArgs.BasicProperties.Headers : null;
        public override ClaimsPrincipal? User { get; set; }
        internal object? Result { get; set; }
#endif

    }
}