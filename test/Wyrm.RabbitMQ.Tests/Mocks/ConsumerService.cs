using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wyrm.Events;

namespace Wyrm.RabbitMQ.Tests.Mocks
{
    [Event("Test.Queue")]
    public class ConsumerService<T> : EventHandler<T, string> where T : class
    {
        private readonly ICollection<T> handledMessages = new List<T>();
        public IEnumerable<T> HandledMessages => handledMessages;

        public override Task<string> HandleAsync(T payload, CancellationToken token)
        {
            handledMessages.Add(payload);
            return Task.FromResult(payload + " + handled!");
        }

        public void ClearHandledMessaged()
        {
            handledMessages.Clear();
        }
    }
}
