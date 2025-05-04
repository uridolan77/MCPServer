using System;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Data.DataSeeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    public class DatabaseInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(
            IServiceProvider serviceProvider,
            ILogger<DatabaseInitializer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<McpServerDbContext>();

                // Check if database exists, create if not
                _logger.LogInformation("Ensuring database exists...");
                await dbContext.Database.EnsureCreatedAsync();

                // We'll skip migrations for now due to compatibility issues
                // await dbContext.Database.MigrateAsync();

                // Additional API key seeding can be done here
                // This allows us to set API keys without storing them in the code
                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogInformation("Setting OpenAI API key from environment variable");
                    dbContext.SeedLlmProviderCredential(apiKey);
                }

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }
    }
}
