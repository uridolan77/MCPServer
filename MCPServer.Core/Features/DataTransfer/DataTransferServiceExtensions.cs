using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MCPServer.Core.Features.DataTransfer.Services;

namespace MCPServer.Core.Features.DataTransfer
{
    public static class DataTransferServiceExtensions
    {
        public static IServiceCollection AddDataTransferServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register our data transfer services
            services.AddTransient<DataMigrationService>();
            services.AddTransient<DataValidationService>();
            
            // Register the scheduled service if enabled
            var isScheduledMigrationEnabled = configuration.GetValue<bool>("DataTransfer:Settings:EnableScheduledMigration", false);
            if (isScheduledMigrationEnabled)
            {
                services.AddHostedService<DataMigrationScheduleService>();
            }
            
            return services;
        }
    }
}