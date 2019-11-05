using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wyrm.RabbitMQ.Tests.Mocks;

namespace Wyrm.RabbitMQ.Tests
{
    public class BasicTests
    {

        private readonly ConsumerService<string> consumer = new ConsumerService<string>();

        [SetUp]
        public void Setup()
        {
            consumer.ClearHandledMessaged();
        }

        /// <summary>
        /// Creates application using a mock queueservice that dispatches 
        /// the provided messages
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IHostBuilder CreateApp<TPayload>(IEnumerable<TPayload> messages)
        where TPayload : class
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddWyrm(options =>
                    {
                        options.UseRabbitMq("rabbitmq")

                        .AddEventHandler(implementationFactory: s => consumer);
                    });

                    services.AddScoped(s => MockQueueService.CreateQueueService(
                        messages: messages));

                });
        }

        [TestCase("Test Message")]
        [TestCase("Test Message 1", "Test Message 2")]
        [TestCase("Test Message 1", "Test Message 2", "Message 3")]
        public void StringMessages(params string[] messages)
        {
            var app = CreateApp(messages);
            app.RunConsoleAsync().Wait(1);
            consumer.HandledMessages.Should().HaveCount(messages.Length);
            int i = 0;
            foreach (var handledMessage in consumer.HandledMessages)
            {

                messages[i++].Should().Be(handledMessage);
            }
        }

    }
}
