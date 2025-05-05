using System;
using MCPServer.Core.Services.Interfaces;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Llm;
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

            // Register HttpClient for LLM service
            services.AddHttpClient<ILlmService, LlmService>();

            // Register LLM services and factories
            services.AddHttpClient("OpenAI");
            services.AddHttpClient("Anthropic");
            services.AddScoped<ILlmProviderFactory, OpenAiProviderFactory>();
            services.AddScoped<ILlmProviderFactory, AnthropicProviderFactory>();
            services.AddScoped<ILlmService, LlmService>();

            return services;
        }
    }
}
