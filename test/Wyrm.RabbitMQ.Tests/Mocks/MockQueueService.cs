using Moq;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wyrm.RabbitMq.Extentions.DependencyInjection;

namespace Wyrm.RabbitMQ.Tests.Mocks
{
    public class MockQueueService
    {
        public static IQueueService CreateQueueService<TPayload>(IEnumerable<TPayload> messages)
            where TPayload : class
        {
            // replace the real QueueService with a fake
            var mock = new Mock<IQueueService>();
            mock.Setup(qs => qs.ReceiveMessages(It.IsAny<string>(),
                It.IsAny<Func<BasicDeliverEventArgs, Task>>(),
                It.IsAny<CancellationToken>()))
                .Callback((string queueName, Func<BasicDeliverEventArgs, Task> callback, CancellationToken token) =>
                {
                    foreach (var message in messages)
                    {
                        callback(CreateTestMessage(message)).Wait();
                    }
                    //callback(CreateTestMessage()).Wait();
                }).Verifiable();

            mock.Setup(qs => qs.CreateExchangeQueue(It.IsAny<string>()))
                .Returns("TempQueue");

            return mock.Object;
        }


        private static BasicDeliverEventArgs CreateTestMessage<TPayload>(TPayload? message)
            where TPayload : class
        {
            if (message == null)
            {
                return new BasicDeliverEventArgs();
            }
            else if (message is string s)
            {
                return new BasicDeliverEventArgs()
                {
                    Body = Encoding.UTF8.GetBytes(s)
                };
            }
            else
            {
                return new BasicDeliverEventArgs
                {
                    Body = Encoding.UTF8.GetBytes(
                        JsonConvert.SerializeObject(message))
                };
            }
        }
    }
}
