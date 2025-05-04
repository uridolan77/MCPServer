using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data
{
    public class DatabaseInitializer
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly LlmProviderSeeder _llmProviderSeeder;

        public DatabaseInitializer(
            McpServerDbContext dbContext,
            ILogger<DatabaseInitializer> logger,
            LlmProviderSeeder llmProviderSeeder)
        {
            _dbContext = dbContext;
            _logger = logger;
            _llmProviderSeeder = llmProviderSeeder;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization");

                // Apply migrations
                await _dbContext.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully");

                // Seed data
                await _llmProviderSeeder.SeedAsync();
                _logger.LogInformation("LLM provider data seeded successfully");

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
