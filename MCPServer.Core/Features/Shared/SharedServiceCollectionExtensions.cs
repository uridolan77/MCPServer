using System;
using MCPServer.Core.Features.Shared.Services;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Shared
{
    /// <summary>
    /// Extension methods for setting up shared services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class SharedServiceCollectionExtensions
    {
        /// <summary>
        /// Adds shared services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddSharedServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register shared services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISecurityService, SecurityService>();

            // Fix for Repository<T> constructor - create a non-generic ILogger that delegates to a generic one
            services.AddTransient<Microsoft.Extensions.Logging.ILogger>(provider => 
                provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Repository<object>>>());

            return services;
        }

        /// <summary>
        /// Adds repository services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="T">The entity type for the repository.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRepository<T>(this IServiceCollection services) where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<IRepository<T>, Repository<T>>();

            return services;
        }
    }
}
