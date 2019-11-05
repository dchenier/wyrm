using System;
using Wyrm.Events;
using Wyrm.Events.Builder;
using Wyrm.Events.Hosting;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWyrm(this IServiceCollection services,
            Action<WyrmOptionsBuilder> options)
        {
            services.AddTransient<IEventBuilder, EventBuilder>();

            var optionsBuilder = new WyrmOptionsBuilder(services);
            options(optionsBuilder);

            return services;
        }
    }
}