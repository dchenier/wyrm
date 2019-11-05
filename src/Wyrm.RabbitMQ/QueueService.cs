
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class QueueService : IQueueService, IDisposable
    {
        private readonly QueueServiceOptions _options;
        private readonly ILogger<QueueService> _logger;

        public QueueService(IOptions<QueueServiceOptions> options,
            ILogger<QueueService> logger)
        {
            if (options == null || options.Value == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

#if NETSTANDARD2_0
        private IConnection connection;
        private IModel channel;
#else
        private IConnection? connection;
        private IModel? channel;
#endif

        private IModel EnsureChannel()
        {
            if (string.IsNullOrWhiteSpace(_options.ServiceBusHostName))
            {
                throw new InvalidOperationException("RabbitMQ host name has not beenset");
            }

            if (connection == null)
            {
                var factory = new ConnectionFactory() { HostName = _options.ServiceBusHostName };
                if (_options.ServiceBusPort > 0)
                {
                    factory.Port = _options.ServiceBusPort;
                }

                connection = factory.CreateConnection();
                _logger.LogInformation("RabbitMQ connection created");
            }

            if (channel == null)
            {
                channel = connection.CreateModel();
                _logger.LogInformation("RabbitMQ channel created");
            }

            return channel;
        }

        /// <summary>
        /// Creates a RabbitMQ channel using default if it
        /// doesn't already exist
        /// </summary>
        public void EnsureQueue(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentNullException(nameof(queueName));

            _logger.LogInformation("Creating queue {QueueName}", queueName);

            var chnl = EnsureChannel();

            chnl.QueueDeclare(queue: queueName,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            chnl.BasicQos(0, 1, false);
       }

       public void EnsureExchange(string exchangeName)
       {
            _logger.LogInformation("Creating exchange {ExchangeName}", exchangeName);
            EnsureChannel();

            channel.ExchangeDeclare(exchange: exchangeName,
                                    type: "fanout");
       }

        public string CreateExchangeQueue(string exchangeName)
        {
            _logger.LogInformation("Creating exchange queue {ExchangeName}", exchangeName);
            EnsureChannel();

            // ensure exchange exists
            EnsureExchange(exchangeName);

            var queueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: queueName,
                  exchange: exchangeName,
                  routingKey: "");

            return queueName;
        }


        public void ReceiveMessages(string queueName,
            Func<BasicDeliverEventArgs, Task> onMessageReceivedAsync,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(queueName))
                throw new ArgumentNullException(nameof(queueName));
            if (onMessageReceivedAsync == null)
                throw new ArgumentNullException(nameof(onMessageReceivedAsync));
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            EnsureChannel();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                // try
                // {
                    onMessageReceivedAsync(ea).Wait(cancellationToken);
                // }
                // finally
                // {
                //     channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                // }

            };

            channel.BasicConsume(queue: queueName,
                autoAck: true,
                consumer: consumer);
        }


        public void StopReceivingMessages()
        {
            if (channel != null)
            {
                channel.Close();
                try { channel.Dispose(); }
                catch (Exception) { /* don't care */}
                finally { channel = null; }
            }

            if (connection != null) 
            { 
                connection.Close();
                try { connection.Dispose(); }
                catch (Exception) { /* don't care */}
                finally { connection = null; }
            }
        }

        private static readonly JsonSerializerOptions defaultJsonSerializerOptions =
            new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        internal static JsonSerializerOptions DefaultJsonSerializerOptions => defaultJsonSerializerOptions;

        public void SendMessage(string queueName,
            object payload,
#if NETSTANDARD2_0
            Action<IBasicProperties> options = null)
#else
            Action<IBasicProperties>? options = null)
#endif
        {
            var watch = new Stopwatch();

            string body = payload == null ? "" : JsonSerializer.Serialize(payload, DefaultJsonSerializerOptions);

            _logger.LogInformation("Sending message to queue {Queue}", queueName);

            var chnl = EnsureChannel();
            IBasicProperties props = chnl.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>();

            options?.Invoke(props);

            chnl.BasicPublish(exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body == null ? null : Encoding.UTF8.GetBytes(body));

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Sent message in {ElapsedMilliseconds}ms",
                    watch.ElapsedMilliseconds);
            }
        }


        public void SendExchangeMessage(string exchangeName,
            object payload,
#if NETSTANDARD2_0
            Action<IBasicProperties> options = null)
#else
            Action<IBasicProperties>? options = null)
#endif
        {
            var watch = new Stopwatch();

            string body = payload == null ? "" : JsonSerializer.Serialize(payload,
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            _logger.LogInformation("Sending message to exchange {Exchange}", exchangeName);

            var chnl = EnsureChannel();
            IBasicProperties props = chnl.CreateBasicProperties();
            props.Headers = new Dictionary<string, object>();

            options?.Invoke(props);

            chnl.BasicPublish(exchange: exchangeName,
                routingKey: "",
                mandatory: false,
                basicProperties: props,
                body: body == null ? null : Encoding.UTF8.GetBytes(body));

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Sent message in {ElapsedMilliseconds}ms",
                    watch.ElapsedMilliseconds);
            }        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (channel != null)
                    {
                        try { channel.Dispose(); }
                        catch (Exception) { /* don't care */}
                        finally { channel = null; }
                    }

                    if (connection != null)
                    {
                        try { connection.Dispose(); }
                        catch (Exception) { /* don't care */}
                        finally { connection = null; }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CampaignService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}