using System;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Core.Features.Rag
{
    /// <summary>
    /// Extension methods for setting up RAG services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class RagServiceCollectionExtensions
    {
        /// <summary>
        /// Adds RAG services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRagServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Register VectorDb service that wasn't removed
            services.AddScoped<IVectorDbService, MCPServer.Core.Services.Rag.MySqlVectorDbService>();
            
            // Note: The following services were removed in the cleanup:
            // - EmbeddingService
            // - MySqlDocumentService
            // - RagService

            return services;
        }
    }
}
