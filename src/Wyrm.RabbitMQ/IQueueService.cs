
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    public interface IQueueService : IDisposable
    {
        /// <summary>
        /// Creates a RabbitMQ channel using default if it
        /// doesn't already exist
        /// </summary>
        void EnsureQueue(string queueName);

        void EnsureExchange(string exchangeName);

        /// <summary>
        /// Creates a temporary queue and binds it to an exchange
        /// </summary>
        /// <returns>
        /// The name of the temporary queue
        /// </returns>
        string CreateExchangeQueue(string exchangeName);

        void ReceiveMessages(string queueName,
            Func<BasicDeliverEventArgs, Task> onMessageReceived,
            CancellationToken cancellationToken);

        void StopReceivingMessages();

        void SendMessage(string queueName,
            object payload,
#if NETSTANDARD2_0
            Action<IBasicProperties> options = null);
#else
            Action<IBasicProperties>? options = null);
#endif

        void SendExchangeMessage(string exchangeName,
            object payload,
#if NETSTANDARD2_0
            Action<IBasicProperties> options = null);
#else
            Action<IBasicProperties>? options = null);
#endif
    }
}