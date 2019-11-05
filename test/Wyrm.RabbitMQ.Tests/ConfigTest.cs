using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Wyrm.RabbitMq.Extentions.DependencyInjection;
using Wyrm.RabbitMQ.Tests.Mocks;

namespace Wyrm.RabbitMQ.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TaskQueueTest()
        {
            var consumer = new ConsumerService();

            var app = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddWyrm(options =>
                    {
                        options.UseRabbitMq("rabbitmq")

                        .AddEventHandler(implementationFactory: s => consumer);
                    });

                    services.AddScoped(s => MockQueueService.CreateQueueService(
                        messages: new string[] 
                        { 
                            "Message 1",
                            "Message 2"
                        }));

                })
                .RunConsoleAsync();

            // Wait 1 ms for the queueService to produce its single message
            app.Wait(1);

            consumer.HandledMessages.Should().HaveCount(2);
        }
    }
}