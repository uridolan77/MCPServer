using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.DataTransfer.Services
{
    public class DataMigrationScheduleService : BackgroundService
    {
        private readonly ILogger<DataMigrationScheduleService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public DataMigrationScheduleService(
            ILogger<DataMigrationScheduleService> logger,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Migration Schedule Service is starting");

            // Get interval from configuration (default to 10 minutes)
            var intervalMinutes = _configuration.GetValue<int>("DataTransfer:Settings:ScheduleIntervalMinutes", 10);
            var interval = TimeSpan.FromMinutes(intervalMinutes);

            _timer = new Timer(DoMigration, null, TimeSpan.Zero, interval);

            return Task.CompletedTask;
        }

        private async void DoMigration(object state)
        {
            _logger.LogInformation("Starting scheduled data migration");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var migrationService = scope.ServiceProvider.GetRequiredService<DataMigrationService>();
                    await migrationService.RunMigrationAsync();
                    
                    // Optionally validate the results
                    var shouldValidate = _configuration.GetValue<bool>("DataTransfer:Settings:ValidateAfterMigration", false);
                    if (shouldValidate)
                    {
                        var validationService = scope.ServiceProvider.GetRequiredService<DataValidationService>();
                        var results = await validationService.ValidateAsync(migrationService.GetProcessedTables());
                        
                        // Log validation results
                        foreach (var result in results)
                        {
                            if (result.Success)
                            {
                                _logger.LogInformation("Validation successful: {Type} for {Table}", 
                                    result.ValidationType, result.TableName);
                            }
                            else
                            {
                                _logger.LogWarning("Validation failed: {Type} for {Table}: {Error}", 
                                    result.ValidationType, result.TableName, result.ErrorMessage);
                            }
                        }
                    }
                }

                _logger.LogInformation("Scheduled data migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during scheduled data migration");
            }
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Migration Schedule Service is stopping");

            _timer?.Change(Timeout.Infinite, 0);

            return base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}