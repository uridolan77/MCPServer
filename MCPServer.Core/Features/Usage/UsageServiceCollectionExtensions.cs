using System;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Usage
{
    /// <summary>
    /// Extension methods for setting up usage services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class UsageServiceCollectionExtensions
    {
        /// <summary>
        /// Adds usage services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddUsageServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register usage services
            services.AddScoped<IChatUsageService, ChatUsageService>();

            return services;
        }
    }
}
