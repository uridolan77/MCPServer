using System;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Models
{
    /// <summary>
    /// Extension methods for setting up model services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ModelsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds model services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddModelServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register model services
            services.AddScoped<IModelService, MCPServer.Core.Services.ModelService>();

            return services;
        }
    }
}
