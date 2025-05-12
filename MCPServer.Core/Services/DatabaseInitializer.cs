using System;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    // Adapter class that forwards to the new implementation in Features namespace
    public class DatabaseInitializer
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(
            McpServerDbContext dbContext,
            ILogger<DatabaseInitializer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing database");
                
                // Check if database exists, if not create it
                await _dbContext.Database.EnsureCreatedAsync();
                
                // Apply any pending migrations
                if (_dbContext.Database.IsRelational())
                {
                    try
                    {
                        _logger.LogInformation("Applying database migrations");
                        await _dbContext.Database.MigrateAsync();
                        _logger.LogInformation("Database migrations applied successfully");
                    }
                    catch (Exception migrateEx)
                    {
                        _logger.LogError(migrateEx, "Error applying database migrations. Will attempt to continue without migrations.");
                        // Don't throw the exception here - try to continue without migrations
                    }
                }
                
                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing database");
                throw;
            }
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Seeding database with initial data");
                
                // Add seeding logic here if needed
                
                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding database");
                throw;
            }
        }
    }
}