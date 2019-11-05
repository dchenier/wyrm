using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wyrm.Events.Builder;

namespace Wyrm.Events.Hosting
{
    // The Generic type is only here so that when we call AddHostedServive<T> it
    // registers different classes to the DI framework.
    internal class EventHandlerHostedService<T> : IHostedService, IDisposable
    {
        private readonly ICollection<IEventService> _eventServices;
        private readonly EventDelegate _eventDelegate;

#if NETSTANDARD2_0
        private ICollection<Task> _executingTasks;
#else
        private ICollection<Task>? _executingTasks;
#endif   
        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();

        public EventHandlerHostedService(
            IEventService eventService,
            EventDelegate eventDelegate) 
        {
            if (eventService == null)
                throw new ArgumentNullException(nameof(eventService));
            _eventDelegate = eventDelegate ?? throw new ArgumentNullException(nameof(eventDelegate));

            _eventServices = new List<IEventService>() { eventService };
        }

        public EventHandlerHostedService(IEnumerable<IEventService> eventServices,
            EventDelegate eventDelegate)
        {
            if (eventServices == null)
                throw new ArgumentNullException(nameof(eventServices));

            _eventServices = eventServices.ToList();
            _eventDelegate = eventDelegate ?? throw new ArgumentNullException(nameof(eventDelegate));
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            /// pattern from https://docs.microsoft.com/en-us/dotnet/architecture/microservices/multi-container-microservice-net-applications/background-tasks-with-ihostedservice

            _executingTasks = _eventServices.Select(e => {
                return e.StartAsync(ctx => {
                    ctx.ServiceAborted = cancellationToken;
                    return _eventDelegate(ctx);
                }, _stoppingCts.Token);
            }).ToList();

            var allTasks = Task.WhenAny(_executingTasks);

            // If any of the tasks are completed then return the aggregate collection of taks,
            // this will bubble cancellation and failure to the caller
            if (allTasks.IsCompleted)
            {
                return allTasks;
            }

            // Otherwise it's running
            return Task.CompletedTask;            
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            //return _eventService.StopAsync(cancellationToken);

            // Stop called without start
            if (_executingTasks == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until all the tasks complete or the stop token triggers
                await Task.WhenAny(Task.WhenAll(_executingTasks), 
                    Task.Delay(Timeout.Infinite,
                    cancellationToken));
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _stoppingCts.Cancel();
                    if (_eventServices != null)
                    {
                        foreach (var eventService in _eventServices)
                        {
                           var disposableEventService = eventService as IDisposable;
                            if (disposableEventService != null)
                            {
                                try { disposableEventService.Dispose(); }
                                catch (Exception) { /* eat errors here */ }
                            } 
                        }
                    }

                    
                }
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EventHandlerHostedService()
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