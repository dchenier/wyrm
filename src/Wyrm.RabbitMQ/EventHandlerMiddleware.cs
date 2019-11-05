using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wyrm.Events;
using Wyrm.Events.Builder;

namespace Wyrm.RabbitMq.Extentions.DependencyInjection
{
    internal class EventHandlerMiddleware<THandler>
        where THandler : class, IEventHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventHandlerMiddleware<THandler>> _logger;
        public EventHandlerMiddleware(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // we'll allow the logger to be null
            _logger = _serviceProvider.GetService<ILogger<EventHandlerMiddleware<THandler>>>();
        }

        private void LogError(Exception error, string message, params object[] args)
        {
            if (_logger != null)
            {
                _logger.LogError(error, message, args);
            }
        }

        public async Task Handle(EventContext context, Func<Task> next)
        {
            if (!(context is RabbitMessageContext rabbitContext))
            {
                throw new InvalidOperationException("Expected context to be of type RabbitMessageContext");
            }
#pragma warning disable IDE0063 // Use simple 'using' statement
            using (var scope = _serviceProvider.CreateScope())
#pragma warning restore IDE0063 // Use simple 'using' statement
            {
                var eventHandler = scope.ServiceProvider.GetRequiredService<THandler>();
                eventHandler.Context = context;

                var (inputType, outputType, handlerMethod) = GetHandlerInfo();

                var payload = ConvertPayload(rabbitContext, inputType);

                if (outputType == null)
                {
                    var invokeHandlerMethod = typeof(EventHandlerMiddleware<THandler>)
                        .GetMethod("InvokeHandlerWithVoidResult", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (invokeHandlerMethod == null)
                    {
                        throw new InvalidOperationException("Unable to get InvokeHandlerWithVoidResult method");

                    }


                    if (!(invokeHandlerMethod.MakeGenericMethod(new Type[] { inputType })?
                        .Invoke(this,
#if NETSTANDARD2_0
                            new object[]
#else
                            new object?[]
#endif
                            {
                                eventHandler,
                                payload,
                                default(CancellationToken)
                            }) is Task task))
                    {
                        throw new InvalidOperationException("InvokeHandlerWithVoidResult result does not appear to be a Task");
                    }

                    await task;
                }
                else
                {
                    var invokeHandlerMethod = typeof(EventHandlerMiddleware<THandler>)
                        .GetMethod("InvokeHandler", BindingFlags.Instance | BindingFlags.NonPublic);

                    if (invokeHandlerMethod == null)
                    {
                        throw new InvalidOperationException("Unable to find InvokeHandler method");
                    }

                    if (!(invokeHandlerMethod.MakeGenericMethod(new Type[] { inputType, outputType })
                        .Invoke(this,
#if NETSTANDARD2_0
                            new object[] 
#else
                            new object?[] 
#endif
                            {
                                eventHandler,
                                payload,
                                default(CancellationToken)
                            }) is Task<object> task))
                    {
                        throw new InvalidOperationException("InvokeHandler result does not appear to be a Task");
                    }

                    var result = await task;

                    rabbitContext.Result = result;
                }

                if (next != null)
                    await next();
            }
        }

#if NETSTANDARD2_0
        private (Type, Type, MethodInfo) GetHandlerInfo()
#else
        private (Type, Type?, MethodInfo) GetHandlerInfo()
#endif   
        {
            var eventHandlerTypeWithoutReturnValue = typeof(THandler).GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>))
                .FirstOrDefault();

            var eventHandlerTypeWithReturnValue = typeof(THandler).GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<,>))
                .FirstOrDefault();

            var eventHandlerType = eventHandlerTypeWithoutReturnValue ?? eventHandlerTypeWithReturnValue;

            if (eventHandlerType == null)
                throw new WyrmConfigurationException($"Handler of type {typeof(THandler).FullName} does not implement {typeof(IEventHandler<>).FullName}");

            var handlerTypes = eventHandlerType.GetGenericArguments();
            if (handlerTypes.Length == 0 && handlerTypes.Length > 2)
                throw new InvalidOperationException($"Expected type {typeof(THandler).FullName} to have 1 or 2 generic arguments, got {handlerTypes.Length}");

            var handlerMethod = eventHandlerType.GetMethod("HandleAsync");
            if (handlerMethod == null)
                throw new InvalidOperationException("Could not find handler method in type {typeof(THandler).FullName}");

            return (handlerTypes[0], handlerTypes.Length == 2 ? handlerTypes[1] : null, handlerMethod);
        }


#if NETSTANDARD2_0
        private static object ConvertPayload(RabbitMessageContext context, Type payloadType)
#else
        private static object? ConvertPayload(RabbitMessageContext context, Type payloadType)
#endif   
        {
            byte[] payload = context.EventArgs.Body;
            if (payload == null)
                return null;

            if (payloadType == typeof(byte[]))
            {
                // easiest case, but we have to box the payload
                return (object)payload;
            }
            else if (payloadType == typeof(Stream))
            {
                // we'll put it into a memory stream
                return new MemoryStream(payload);
            }
            else
            {
                // otherwise we'll assume there's text in the payload, in UTF8 format.
                if (payloadType == typeof(string))
                {
                    return Encoding.UTF8.GetString(payload);
                }
                else
                {
                    // if here we should have an object.
                    // in theory we could maybe inspect a "Content-Type" header and try to deserialize based on that
                    // but...we won't worry about that now.  let's just assume we have json and want to deserialize that.
                    try
                    {
                        return JsonSerializer.Deserialize(payload, payloadType, QueueService.DefaultJsonSerializerOptions);
                    }
                    catch (Exception err)
                    {
                        throw new DeserializeMessageException("Unable to deserialize message payload to type " + payloadType.FullName + ", see innerException for details", err);
                    }
                }
            }
        }

#pragma warning disable IDE0051 // Remove unused private members
        private async Task InvokeHandlerWithVoidResult<TModel>(
#pragma warning restore IDE0051 // Remove unused private members
            IEventHandler<TModel> handler,
#if NETSTANDARD2_0
            TModel payload,
#else
            TModel? payload, 
#endif
            CancellationToken cancellationToken)
            where TModel : class
        {
            try
            {
#if NETSTANDARD2_0
                await handler.HandleAsync(payload, cancellationToken);
#else
                await handler.HandleAsync(payload!, cancellationToken);
#endif
            }
            catch (Exception err)
            {
                LogError(err, "Wyrm Handler error: {ErrorMessage}", err);
                throw;
            }
        }

#pragma warning disable IDE0051 // Remove unused private members
#if NETSTANDARD2_0
        private async Task<object> InvokeHandler<TModel, TResult>(
#else
        private async Task<object?> InvokeHandler<TModel, TResult>(
#endif
#pragma warning restore IDE0051 // Remove unused private members
            IEventHandler<TModel, TResult> handler,
            TModel payload,
            CancellationToken cancellationToken)
            where TModel : class
        {
            try
            {
                return await handler.HandleAsync(payload, cancellationToken);
            }
            catch (Exception err)
            {
                LogError(err, "Wyrm Handler error: {ErrorMessage}", err);
                throw;
            }

            // TODO: do something with the result
        }
    }
}