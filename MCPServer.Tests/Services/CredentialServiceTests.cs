using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Services
{
    public class CredentialServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ISecurityService> _securityServiceMock;
        private readonly Mock<ILogger<CredentialService>> _loggerMock;
        private readonly Mock<IRepository<LlmProviderCredential>> _repositoryMock;
        private readonly CredentialService _credentialService;

        public CredentialServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _securityServiceMock = new Mock<ISecurityService>();
            _loggerMock = new Mock<ILogger<CredentialService>>();
            _repositoryMock = new Mock<IRepository<LlmProviderCredential>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<LlmProviderCredential>())
                .Returns(_repositoryMock.Object);

            _credentialService = new CredentialService(
                _unitOfWorkMock.Object,
                _securityServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetCredentialsByProviderIdAsync_ReturnsCredentialsWithRedactedSensitiveInfo()
        {
            // Arrange
            var providerId = 1;
            var credentials = new List<LlmProviderCredential>
            {
                new LlmProviderCredential
                {
                    Id = 1,
                    ProviderId = providerId,
                    Name = "Credential 1",
                    EncryptedCredentials = "encrypted-data",
                    ApiKey = "api-key-1"
                },
                new LlmProviderCredential
                {
                    Id = 2,
                    ProviderId = providerId,
                    Name = "Credential 2",
                    EncryptedCredentials = "encrypted-data",
                    ApiKey = "api-key-2"
                }
            };

            var queryable = credentials.AsQueryable();
            _repositoryMock.Setup(r => r.Query())
                .Returns(queryable);

            // Act
            var result = await _credentialService.GetCredentialsByProviderIdAsync(providerId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, c =>
            {
                Assert.Equal("[REDACTED]", c.EncryptedCredentials);
                Assert.Equal("[REDACTED]", c.ApiKey);
            });
        }

        [Fact]
        public async Task GetCredentialsByUserIdAsync_ReturnsUserAndSystemCredentials()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var credentials = new List<LlmProviderCredential>
            {
                new LlmProviderCredential
                {
                    Id = 1,
                    UserId = userId,
                    Name = "User Credential",
                    EncryptedCredentials = "encrypted-data",
                    ApiKey = "api-key-1"
                },
                new LlmProviderCredential
                {
                    Id = 2,
                    UserId = null, // System credential
                    Name = "System Credential",
                    EncryptedCredentials = "encrypted-data",
                    ApiKey = "api-key-2"
                }
            };

            var queryable = credentials.AsQueryable();
            _repositoryMock.Setup(r => r.Query())
                .Returns(queryable);

            // Act
            var result = await _credentialService.GetCredentialsByUserIdAsync(userId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.Name == "User Credential");
            Assert.Contains(result, c => c.Name == "System Credential");
        }

        [Fact]
        public async Task GetDefaultCredentialAsync_ReturnsUserSpecificCredentialFirst()
        {
            // Arrange
            var providerId = 1;
            var userId = Guid.NewGuid();
            var credentials = new List<LlmProviderCredential>
            {
                new LlmProviderCredential
                {
                    Id = 1,
                    ProviderId = providerId,
                    UserId = null, // System credential
                    Name = "System Default",
                    IsDefault = true,
                    IsEnabled = true
                },
                new LlmProviderCredential
                {
                    Id = 2,
                    ProviderId = providerId,
                    UserId = userId,
                    Name = "User Default",
                    IsDefault = true,
                    IsEnabled = true
                }
            };

            var queryable = credentials.AsQueryable();
            _repositoryMock.Setup(r => r.Query())
                .Returns(queryable);

            // Act
            var result = await _credentialService.GetDefaultCredentialAsync(providerId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("User Default", result.Name);
        }

        [Fact]
        public async Task GetDefaultCredentialAsync_ReturnsFallbackToSystemCredential()
        {
            // Arrange
            var providerId = 1;
            var userId = Guid.NewGuid();
            var credentials = new List<LlmProviderCredential>
            {
                new LlmProviderCredential
                {
                    Id = 1,
                    ProviderId = providerId,
                    UserId = null, // System credential
                    Name = "System Default",
                    IsDefault = true,
                    IsEnabled = true
                }
            };

            var queryable = credentials.AsQueryable();
            _repositoryMock.Setup(r => r.Query())
                .Returns(queryable);

            // Act
            var result = await _credentialService.GetDefaultCredentialAsync(providerId, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("System Default", result.Name);
        }

        [Fact]
        public async Task AddCredentialAsync_SetsDefaultForFirstCredential()
        {
            // Arrange
            var providerId = 1;
            var credential = new LlmProviderCredential
            {
                ProviderId = providerId,
                Name = "New Credential"
            };

            var rawCredentials = new { ApiKey = "test-api-key" };
            var encryptedJson = "encrypted-json";

            _repositoryMock.Setup(r => r.Query())
                .Returns(new List<LlmProviderCredential>().AsQueryable());

            _securityServiceMock.Setup(s => s.EncryptAsync(It.IsAny<string>()))
                .ReturnsAsync(encryptedJson);

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<LlmProviderCredential>()))
                .ReturnsAsync((LlmProviderCredential c) => c);

            // Act
            var result = await _credentialService.AddCredentialAsync(credential, rawCredentials);

            // Assert
            Assert.True(result.IsDefault);
            Assert.Equal(encryptedJson, result.EncryptedCredentials);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SetDefaultCredentialAsync_ClearsOtherDefaultsAndSetsNewDefault()
        {
            // Arrange
            var providerId = 1;
            var credentials = new List<LlmProviderCredential>
            {
                new LlmProviderCredential
                {
                    Id = 1,
                    ProviderId = providerId,
                    Name = "Credential 1",
                    IsDefault = true
                },
                new LlmProviderCredential
                {
                    Id = 2,
                    ProviderId = providerId,
                    Name = "Credential 2",
                    IsDefault = false
                }
            };

            var queryable = credentials.AsQueryable();
            _repositoryMock.Setup(r => r.Query())
                .Returns(queryable);

            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<object>()))
                .ReturnsAsync((object id) => credentials.FirstOrDefault(c => c.Id == (int)id));

            // Act
            var result = await _credentialService.SetDefaultCredentialAsync(2);

            // Assert
            Assert.True(result);
            _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<LlmProviderCredential>(c => c.Id == 1 && !c.IsDefault)), Times.Once);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<LlmProviderCredential>(c => c.Id == 2 && c.IsDefault)), Times.Once);
        }

        [Fact]
        public async Task GetDecryptedCredentialsAsync_DecryptsAndUpdatesLastUsed()
        {
            // Arrange
            var credentialId = 1;
            var encryptedCredentials = "encrypted-json";
            var decryptedJson = "{\"ApiKey\":\"test-api-key\"}";
            var credential = new LlmProviderCredential
            {
                Id = credentialId,
                EncryptedCredentials = encryptedCredentials,
                LastUsedAt = DateTime.UtcNow.AddDays(-1)
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(credentialId))
                .ReturnsAsync(credential);

            _securityServiceMock.Setup(s => s.DecryptAsync(encryptedCredentials))
                .ReturnsAsync(decryptedJson);

            // Act
            var result = await _credentialService.GetDecryptedCredentialsAsync<TestCredentials>(credentialId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-api-key", result.ApiKey);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<LlmProviderCredential>(c => c.Id == credentialId)), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateUserAccessAsync_ReturnsTrueForSystemCredential()
        {
            // Arrange
            var credentialId = 1;
            var userId = Guid.NewGuid();
            var credential = new LlmProviderCredential
            {
                Id = credentialId,
                UserId = null // System credential
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(credentialId))
                .ReturnsAsync(credential);

            // Act
            var result = await _credentialService.ValidateUserAccessAsync(credentialId, userId, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateUserAccessAsync_ReturnsTrueForUserOwnedCredential()
        {
            // Arrange
            var credentialId = 1;
            var userId = Guid.NewGuid();
            var credential = new LlmProviderCredential
            {
                Id = credentialId,
                UserId = userId
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(credentialId))
                .ReturnsAsync(credential);

            // Act
            var result = await _credentialService.ValidateUserAccessAsync(credentialId, userId, false);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateUserAccessAsync_ReturnsFalseForOtherUserCredential()
        {
            // Arrange
            var credentialId = 1;
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var credential = new LlmProviderCredential
            {
                Id = credentialId,
                UserId = otherUserId
            };

            _repositoryMock.Setup(r => r.GetByIdAsync(credentialId))
                .ReturnsAsync(credential);

            // Act
            var result = await _credentialService.ValidateUserAccessAsync(credentialId, userId, false);

            // Assert
            Assert.False(result);
        }

        private class TestCredentials
        {
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}
