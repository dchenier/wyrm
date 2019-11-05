using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wyrm.Events.Builder
{
    public delegate Task EventDelegate(EventContext context);
}