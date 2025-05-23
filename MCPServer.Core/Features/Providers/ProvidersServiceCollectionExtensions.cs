using System;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Providers
{
    /// <summary>
    /// Extension methods for setting up provider services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ProvidersServiceCollectionExtensions
    {
        /// <summary>
        /// Adds provider services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddProviderServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Note: LlmProviderService has been removed as part of cleanup
            // Add replacement provider service registrations here if needed

            return services;
        }
    }
}
