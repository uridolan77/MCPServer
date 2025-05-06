using Microsoft.Extensions.DependencyInjection;
using MCPServer.API.Features.DataTransfer.Services;

namespace MCPServer.API.Features.DataTransfer
{
    public static class DataTransferApiExtensions
    {
        public static IServiceCollection AddDataTransferApi(this IServiceCollection services)
        {
            // Register API-specific data transfer services
            services.AddScoped<DataTransferService>();
            
            return services;
        }
    }
}