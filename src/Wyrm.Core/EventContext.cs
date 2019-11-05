using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Wyrm.Events
{
    public abstract class EventContext
    {
#if NETSTANDARD2_0
        public abstract IDictionary<string, object> Headers { get; }
        public virtual ClaimsPrincipal User { get; set; }
#else
        public abstract IDictionary<string, object>? Headers { get; }
        public virtual ClaimsPrincipal? User { get; set; }
#endif
        public virtual CancellationToken ServiceAborted { get; set ;}

    }
}