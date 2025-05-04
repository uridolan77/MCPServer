using System;
using System.Threading.Tasks;
using MCPServer.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Services
{
    public class SecurityServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<SecurityService>> _loggerMock;
        private readonly SecurityService _securityService;

        public SecurityServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<SecurityService>>();

            // Setup configuration to return a test encryption key
            var configurationSectionMock = new Mock<IConfigurationSection>();
            configurationSectionMock.Setup(s => s.Value).Returns("test-encryption-key-minimum-length-32-chars");
            _configurationMock.Setup(c => c["AppSettings:EncryptionKey"]).Returns("test-encryption-key-minimum-length-32-chars");

            _securityService = new SecurityService(_configurationMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetEncryptionKeyAsync_ReturnsConfiguredKey()
        {
            // Act
            var key = await _securityService.GetEncryptionKeyAsync();

            // Assert
            Assert.Equal("test-encryption-key-minimum-length-32-chars", key);
        }

        [Fact]
        public async Task GetEncryptionKeyAsync_ThrowsExceptionWhenKeyNotConfigured()
        {
            // Arrange
            _configurationMock.Setup(c => c["AppSettings:EncryptionKey"]).Returns((string)null);
            _configurationMock.Setup(c => c["ASPNETCORE_ENVIRONMENT"]).Returns("Production");

            var securityService = new SecurityService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => securityService.GetEncryptionKeyAsync());
        }

        [Fact]
        public async Task GetEncryptionKeyAsync_UsesDevelopmentKeyInDevelopment()
        {
            // Arrange
            _configurationMock.Setup(c => c["AppSettings:EncryptionKey"]).Returns((string)null);
            _configurationMock.Setup(c => c["ASPNETCORE_ENVIRONMENT"]).Returns("Development");

            var securityService = new SecurityService(_configurationMock.Object, _loggerMock.Object);

            // Act
            var key = await securityService.GetEncryptionKeyAsync();

            // Assert
            Assert.Equal("dev-encryption-key-minimum-length-32-chars", key);
        }

        [Fact]
        public async Task EncryptAsync_DecryptAsync_RoundTrip()
        {
            // Arrange
            var plainText = "This is a test message to encrypt and decrypt";

            // Act
            var encryptedText = await _securityService.EncryptAsync(plainText);
            var decryptedText = await _securityService.DecryptAsync(encryptedText);

            // Assert
            Assert.NotEqual(plainText, encryptedText);
            Assert.Equal(plainText, decryptedText);
        }

        [Fact]
        public async Task EncryptAsync_ReturnsEmptyStringForEmptyInput()
        {
            // Act
            var encryptedText = await _securityService.EncryptAsync(string.Empty);

            // Assert
            Assert.Equal(string.Empty, encryptedText);
        }

        [Fact]
        public async Task DecryptAsync_ReturnsEmptyStringForEmptyInput()
        {
            // Act
            var decryptedText = await _securityService.DecryptAsync(string.Empty);

            // Assert
            Assert.Equal(string.Empty, decryptedText);
        }

        [Fact]
        public async Task EncryptAsync_ProducesDifferentOutputForSameInput()
        {
            // Arrange
            var plainText = "This is a test message";

            // Act
            var encryptedText1 = await _securityService.EncryptAsync(plainText);
            var encryptedText2 = await _securityService.EncryptAsync(plainText);

            // Assert
            Assert.NotEqual(encryptedText1, encryptedText2);
        }
    }
}
