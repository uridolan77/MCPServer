using System;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Llm
{
    /// <summary>
    /// Extension methods for setting up LLM services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class LlmServiceCollectionExtensions
    {
        /// <summary>
        /// Adds LLM services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddLlmServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register HttpClients for LLM providers
            services.AddHttpClient("OpenAI");
            services.AddHttpClient("Anthropic");
            
            // Note: LlmService has been migrated to a different implementation
            // Register any new LLM services here

            return services;
        }
    }
}
