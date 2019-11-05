using System;
using System.Collections.Generic;
using System.Linq;
using Wyrm.Events.Hosting;

namespace Wyrm.Events.Builder
{
    public class WyrmOptions
    {
        private readonly IReadOnlyDictionary<Type, IWyrmOptionsExtension> _extensions;
        // private readonly IReadOnlyCollection<Type> _eventHandlers;

        public WyrmOptions(): this(new Dictionary<Type, IWyrmOptionsExtension>()) {}

        protected WyrmOptions(IReadOnlyDictionary<Type, IWyrmOptionsExtension> extensions)
        {
            _extensions = extensions ?? throw new ArgumentNullException(nameof(extensions)); 
            // _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
        }

        protected WyrmOptions(WyrmOptions clone)
        {
            _extensions = clone.Extensions.ToDictionary(p => p.GetType(), p => p);
            // _eventHandlers = clone.EventHandlers.ToList();
        }
        

        /// <summary>
        ///     Gets the extensions that store the configured options.
        /// </summary>
        public virtual IEnumerable<IWyrmOptionsExtension> Extensions => _extensions.Values;
        // public virtual IEnumerable<Type> EventHandlers => _eventHandlers.AsEnumerable();

        /// <summary>
        ///     Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
        /// </summary>
        /// <typeparam name="TExtension"> The type of the extension to get. </typeparam>
        /// <returns> The extension, or null if none was found. </returns>
#if NETSTANDARD2_0
        public virtual TExtension FindExtension<TExtension>()
#else
        public virtual TExtension? FindExtension<TExtension>()
#endif    
            where TExtension : class, IWyrmOptionsExtension
            => _extensions.TryGetValue(typeof(TExtension), out var extension) ? (TExtension)extension : null;

        /// <summary>
        ///     Adds the given extension to the underlying options and creates a new
        ///     <see cref="WyrmOptions" /> with the extension added.
        /// </summary>
        /// <typeparam name="TExtension"> The type of extension to be added. </typeparam>
        /// <param name="extension"> The extension to be added. </param>
        /// <returns> The new options instance with the given extension added. </returns>
        public virtual WyrmOptions WithExtension<TExtension>(TExtension extension)
            where TExtension : class, IWyrmOptionsExtension
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            var extensions = Extensions.ToDictionary(p => p.GetType(), p => p);
            extensions[typeof(TExtension)] = extension;

            return Create(extensions);            
        }

        public virtual WyrmOptions WithEventHandler<TEventHandler>(IServiceProvider services, IEventBuilder eventBuilder)
            where TEventHandler : class, IEventHandler
        {
            foreach (var extension in Extensions)
            {
                extension.AddEventHandler<TEventHandler>(services, eventBuilder);
            }

            return this;
            // throw new WyrmConfigurationException("Unable to determine what type of Wyrm provider to use (was a provider configured? e.g. options.AddRabbitMq();)");

            // var eventHandlers = _eventHandlers.ToList();
            // eventHandlers.Add(typeof(TEventHandler));
            // return Create(_extensions, eventHandlers);
        }

        protected virtual WyrmOptions Create(
            IReadOnlyDictionary<Type, IWyrmOptionsExtension> extensions)
        {
            // this should be overridden by WyrmProviders
            return new WyrmOptions(extensions);
        }

        public IEventService CreateEventService<TEventHandler>(IServiceProvider services)
            where TEventHandler: class, IEventHandler
        {
            var service = Extensions.Select(ext => ext.CreateEventService<TEventHandler>(services)).FirstOrDefault(svc => svc != null);
            return service ?? throw new WyrmConfigurationException("Unable to determine what type of Wyrm provider to use (was a provider configured? e.g. options.AddRabbitMq();)"); 
        }

        // internal void InternalConfigureMiddleware(IServiceProvider services, IEventBuilder eventBuilder) 
        // { 
        //     this.ConfigureMiddleware(services, eventBuilder); 
        // }


        // protected virtual void ConfigureMiddleware(IServiceProvider services, IEventBuilder eventBuilder)
        // {
        //     throw new WyrmConfigurationException("Unable to determine what type of Wyrm provider to use (was a provider configured? e.g. options.AddRabbitMq();)");
        // }
    }
}