using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Services
{
    public class RepositoryTests
    {
        private readonly Mock<ILogger<Repository<LlmProvider>>> _loggerMock;
        private readonly DbContextOptions<McpServerDbContext> _options;

        public RepositoryTests()
        {
            _loggerMock = new Mock<ILogger<Repository<LlmProvider>>>();
            _options = new DbContextOptionsBuilder<McpServerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var providers = new List<LlmProvider>
            {
                new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" },
                new LlmProvider { Id = 2, Name = "Provider2", DisplayName = "Provider 2" },
                new LlmProvider { Id = 3, Name = "Provider3", DisplayName = "Provider 3" }
            };

            await context.LlmProviders.AddRangeAsync(providers);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Equal(3, result.Count());
            Assert.Contains(result, p => p.Name == "Provider1");
            Assert.Contains(result, p => p.Name == "Provider2");
            Assert.Contains(result, p => p.Name == "Provider3");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectEntity()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var provider = new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" };
            await context.LlmProviders.AddAsync(provider);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Provider1", result.Name);
            Assert.Equal("Provider 1", result.DisplayName);
        }

        [Fact]
        public async Task AddAsync_AddsEntityToDatabase()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var provider = new LlmProvider { Name = "Provider1", DisplayName = "Provider 1" };

            // Act
            await repository.AddAsync(provider);
            await context.SaveChangesAsync();

            // Assert
            var result = await context.LlmProviders.FirstOrDefaultAsync(p => p.Name == "Provider1");
            Assert.NotNull(result);
            Assert.Equal("Provider 1", result.DisplayName);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEntityInDatabase()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var provider = new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" };
            await context.LlmProviders.AddAsync(provider);
            await context.SaveChangesAsync();

            // Act
            provider.DisplayName = "Updated Provider 1";
            await repository.UpdateAsync(provider);
            await context.SaveChangesAsync();

            // Assert
            var result = await context.LlmProviders.FindAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Updated Provider 1", result.DisplayName);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntityFromDatabase()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var provider = new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" };
            await context.LlmProviders.AddAsync(provider);
            await context.SaveChangesAsync();

            // Act
            await repository.DeleteAsync(1);
            await context.SaveChangesAsync();

            // Assert
            var result = await context.LlmProviders.FindAsync(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task FindAsync_ReturnsMatchingEntities()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var providers = new List<LlmProvider>
            {
                new LlmProvider { Id = 1, Name = "OpenAI", DisplayName = "OpenAI" },
                new LlmProvider { Id = 2, Name = "Anthropic", DisplayName = "Anthropic" },
                new LlmProvider { Id = 3, Name = "Google", DisplayName = "Google" }
            };

            await context.LlmProviders.AddRangeAsync(providers);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.FindAsync(p => p.Name.Contains("o") || p.Name.Contains("O"));

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, p => p.Name == "OpenAI");
            Assert.Contains(result, p => p.Name == "Google");
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrueForExistingEntity()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var provider = new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" };
            await context.LlmProviders.AddAsync(provider);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.ExistsAsync(p => p.Name == "Provider1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalseForNonExistingEntity()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            // Act
            var result = await repository.ExistsAsync(p => p.Name == "NonExistingProvider");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CountAsync_ReturnsCorrectCount()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var repository = new Repository<LlmProvider>(context, _loggerMock.Object);

            var providers = new List<LlmProvider>
            {
                new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" },
                new LlmProvider { Id = 2, Name = "Provider2", DisplayName = "Provider 2" },
                new LlmProvider { Id = 3, Name = "Provider3", DisplayName = "Provider 3" }
            };

            await context.LlmProviders.AddRangeAsync(providers);
            await context.SaveChangesAsync();

            // Act
            var totalCount = await repository.CountAsync();
            var filteredCount = await repository.CountAsync(p => p.Name.StartsWith("Provider1"));

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(1, filteredCount);
        }
    }
}
