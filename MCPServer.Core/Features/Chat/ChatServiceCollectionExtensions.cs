using System;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Chat
{
    /// <summary>
    /// Extension methods for setting up chat services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ChatServiceCollectionExtensions
    {
        /// <summary>
        /// Adds chat services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddChatServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register chat services
            services.AddScoped<IChatStreamingService, MCPServer.Core.Services.ChatStreamingService>();

            return services;
        }
    }
}
