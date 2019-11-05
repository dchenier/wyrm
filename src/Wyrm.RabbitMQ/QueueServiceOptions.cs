namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    public class QueueServiceOptions
    {
        public string ServiceBusHostName { get; set; } = "rabbitmq";
        public int ServiceBusPort { get; set; }
    }
}