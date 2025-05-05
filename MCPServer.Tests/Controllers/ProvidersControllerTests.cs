using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.Providers.Controllers;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class ProvidersControllerTests
    {
        private readonly Mock<ILogger<ProvidersController>> _loggerMock;
        private readonly Mock<ILlmProviderService> _providerServiceMock;
        private readonly ProvidersController _controller;

        public ProvidersControllerTests()
        {
            _loggerMock = new Mock<ILogger<ProvidersController>>();
            _providerServiceMock = new Mock<ILlmProviderService>();
            _controller = new ProvidersController(_loggerMock.Object, _providerServiceMock.Object);
        }

        [Fact]
        public async Task GetAllProviders_ReturnsOkResultWithProviders()
        {
            // Arrange
            var providers = new List<LlmProvider>
            {
                new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" },
                new LlmProvider { Id = 2, Name = "Provider2", DisplayName = "Provider 2" }
            };

            _providerServiceMock.Setup(s => s.GetAllProvidersAsync())
                .ReturnsAsync(providers);

            // Act
            var result = await _controller.GetAllProviders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<LlmProvider>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(providers, response.Data);
        }

        [Fact]
        public async Task GetProviderById_WithValidId_ReturnsOkResultWithProvider()
        {
            // Arrange
            var provider = new LlmProvider { Id = 1, Name = "Provider1", DisplayName = "Provider 1" };

            _providerServiceMock.Setup(s => s.GetProviderByIdAsync(1))
                .ReturnsAsync(provider);

            // Act
            var result = await _controller.GetProviderById(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LlmProvider>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(provider, response.Data);
        }

        [Fact]
        public async Task GetProviderById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _providerServiceMock.Setup(s => s.GetProviderByIdAsync(999))
                .ReturnsAsync((LlmProvider)null);

            // Act
            var result = await _controller.GetProviderById(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LlmProvider>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Null(response.Data);
            Assert.Contains("not found", response.Message);
        }

        [Fact]
        public async Task AddProvider_WithValidProvider_ReturnsOkResultWithAddedProvider()
        {
            // Arrange
            var provider = new LlmProvider { Name = "NewProvider", DisplayName = "New Provider" };
            var addedProvider = new LlmProvider { Id = 1, Name = "NewProvider", DisplayName = "New Provider" };

            _providerServiceMock.Setup(s => s.AddProviderAsync(provider))
                .ReturnsAsync(addedProvider);

            // Act
            var result = await _controller.AddProvider(provider);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LlmProvider>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(addedProvider, response.Data);
            Assert.Contains("added successfully", response.Message);
        }

        [Fact]
        public async Task UpdateProvider_WithValidProvider_ReturnsOkResultWithUpdatedProvider()
        {
            // Arrange
            var provider = new LlmProvider { Id = 1, Name = "UpdatedProvider", DisplayName = "Updated Provider" };
            var updatedProvider = new LlmProvider { Id = 1, Name = "UpdatedProvider", DisplayName = "Updated Provider" };

            _providerServiceMock.Setup(s => s.UpdateProviderAsync(provider))
                .ReturnsAsync(true);
            _providerServiceMock.Setup(s => s.GetProviderByIdAsync(1))
                .ReturnsAsync(updatedProvider);

            // Act
            var result = await _controller.UpdateProvider(1, provider);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LlmProvider>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(updatedProvider, response.Data);
            Assert.Contains("updated successfully", response.Message);
        }

        [Fact]
        public async Task UpdateProvider_WithIdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var provider = new LlmProvider { Id = 2, Name = "Provider2", DisplayName = "Provider 2" };

            // Act
            var result = await _controller.UpdateProvider(1, provider);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LlmProvider>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("mismatch", response.Message);
        }

        [Fact]
        public async Task UpdateProvider_WithNonExistentProvider_ReturnsNotFound()
        {
            // Arrange
            var provider = new LlmProvider { Id = 999, Name = "NonExistentProvider", DisplayName = "Non-Existent Provider" };

            _providerServiceMock.Setup(s => s.UpdateProviderAsync(provider))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateProvider(999, provider);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LlmProvider>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains("not found", response.Message);
        }

        [Fact]
        public async Task DeleteProvider_WithValidId_ReturnsOkResultWithSuccess()
        {
            // Arrange
            _providerServiceMock.Setup(s => s.DeleteProviderAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProvider(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
            Assert.True(response.Success);
            Assert.True(response.Data);
            Assert.Contains("deleted successfully", response.Message);
        }

        [Fact]
        public async Task DeleteProvider_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            _providerServiceMock.Setup(s => s.DeleteProviderAsync(999))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteProvider(999);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<bool>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains("not found", response.Message);
        }

        [Fact]
        public async Task GetAllProviders_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            _providerServiceMock.Setup(s => s.GetAllProvidersAsync())
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetAllProviders();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ApiResponse<IEnumerable<LlmProvider>>>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Error getting all providers", response.Message);
        }
    }
}
