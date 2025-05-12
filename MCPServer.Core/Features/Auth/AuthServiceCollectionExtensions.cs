using System;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Auth
{
    /// <summary>
    /// Extension methods for setting up authentication services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class AuthServiceCollectionExtensions
    {
        /// <summary>
        /// Adds authentication services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddAuthServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register authentication services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenManager, TokenManager>();

            return services;
        }
    }
}
