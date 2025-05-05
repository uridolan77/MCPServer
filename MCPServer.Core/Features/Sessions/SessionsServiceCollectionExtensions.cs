using System;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Sessions
{
    /// <summary>
    /// Extension methods for setting up session services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class SessionsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds session services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddSessionServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register session services
            services.AddScoped<ISessionService, MCPServer.Core.Services.SessionService>();
            services.AddScoped<IContextService, MCPServer.Core.Services.MySqlContextService>();

            return services;
        }
    }
}
