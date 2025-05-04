using System;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Services
{
    public class UnitOfWorkTests
    {
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly DbContextOptions<McpServerDbContext> _options;

        public UnitOfWorkTests()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _options = new DbContextOptionsBuilder<McpServerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Setup logger factory to return a mock logger
            var loggerMock = new Mock<ILogger<Repository<LlmProvider>>>();
            _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(loggerMock.Object);
        }

        [Fact]
        public void GetRepository_ReturnsSameRepositoryForSameType()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

            // Act
            var repository1 = unitOfWork.GetRepository<LlmProvider>();
            var repository2 = unitOfWork.GetRepository<LlmProvider>();

            // Assert
            Assert.Same(repository1, repository2);
        }

        [Fact]
        public void GetRepository_ReturnsDifferentRepositoriesForDifferentTypes()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

            // Act
            var repository1 = unitOfWork.GetRepository<LlmProvider>();
            var repository2 = unitOfWork.GetRepository<LlmModel>();

            // Assert
            Assert.NotSame(repository1, repository2);
        }

        [Fact]
        public async Task SaveChangesAsync_PersistsChangesToDatabase()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

            var repository = unitOfWork.GetRepository<LlmProvider>();
            var provider = new LlmProvider { Name = "TestProvider", DisplayName = "Test Provider" };

            // Act
            await repository.AddAsync(provider);
            await unitOfWork.SaveChangesAsync();

            // Assert
            using var newContext = new McpServerDbContext(_options);
            var savedProvider = await newContext.LlmProviders.FirstOrDefaultAsync(p => p.Name == "TestProvider");
            Assert.NotNull(savedProvider);
            Assert.Equal("Test Provider", savedProvider.DisplayName);
        }

        [Fact]
        public async Task BeginTransactionAsync_CommitTransactionAsync_CommitsChanges()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

            var repository = unitOfWork.GetRepository<LlmProvider>();
            var provider = new LlmProvider { Name = "TransactionProvider", DisplayName = "Transaction Provider" };

            // Act
            await unitOfWork.BeginTransactionAsync();
            await repository.AddAsync(provider);
            await unitOfWork.CommitTransactionAsync();

            // Assert
            using var newContext = new McpServerDbContext(_options);
            var savedProvider = await newContext.LlmProviders.FirstOrDefaultAsync(p => p.Name == "TransactionProvider");
            Assert.NotNull(savedProvider);
            Assert.Equal("Transaction Provider", savedProvider.DisplayName);
        }

        [Fact]
        public async Task BeginTransactionAsync_RollbackTransactionAsync_DiscardsChanges()
        {
            // Arrange
            using var context = new McpServerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

            var repository = unitOfWork.GetRepository<LlmProvider>();
            var provider = new LlmProvider { Name = "RollbackProvider", DisplayName = "Rollback Provider" };

            // Act
            await unitOfWork.BeginTransactionAsync();
            await repository.AddAsync(provider);
            await unitOfWork.RollbackTransactionAsync();

            // Assert
            using var newContext = new McpServerDbContext(_options);
            var savedProvider = await newContext.LlmProviders.FirstOrDefaultAsync(p => p.Name == "RollbackProvider");
            Assert.Null(savedProvider);
        }

        [Fact]
        public async Task Dispose_DisposesContextAndTransaction()
        {
            // Arrange
            var context = new McpServerDbContext(_options);
            var unitOfWork = new UnitOfWork(context, _loggerFactoryMock.Object);

            // Start a transaction
            await unitOfWork.BeginTransactionAsync();

            // Act
            unitOfWork.Dispose();

            // Assert
            // This will throw if the context has been disposed
            Assert.Throws<ObjectDisposedException>(() => context.LlmProviders.Add(new LlmProvider()));
        }
    }
}
