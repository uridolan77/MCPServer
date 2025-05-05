using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.Usage.Controllers;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Responses;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class UsageControllerTests
    {
        private readonly Mock<IChatUsageService> _mockChatUsageService;
        private readonly Mock<ILogger<UsageController>> _mockLogger;
        private readonly UsageController _controller;

        public UsageControllerTests()
        {
            _mockChatUsageService = new Mock<IChatUsageService>();
            _mockLogger = new Mock<ILogger<UsageController>>();
            _controller = new UsageController(_mockChatUsageService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetOverallStats_ReturnsSuccessResponse()
        {
            // Arrange
            var stats = new ChatUsageStatsResponse
            {
                TotalMessages = 100,
                TotalTokensUsed = 50000,
                TotalCost = 10.5m,
                ModelStats = new List<ModelUsageStatResponse>(),
                ProviderStats = new List<ProviderUsageStatResponse>()
            };

            _mockChatUsageService.Setup(x => x.GetOverallStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var result = await _controller.GetOverallStats();

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ChatUsageStatsResponse>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ChatUsageStatsResponse>>(okResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Usage statistics retrieved successfully", apiResponse.Message);
            Assert.Equal(stats, apiResponse.Data);
        }

        [Fact]
        public async Task GetFilteredLogs_ReturnsSuccessResponse()
        {
            // Arrange
            var logs = new List<ChatUsageLogResponse>
            {
                new ChatUsageLogResponse
                {
                    Id = 1,
                    SessionId = "session1",
                    ModelName = "gpt-4",
                    ProviderName = "OpenAI",
                    InputTokens = 500,
                    OutputTokens = 300,
                    EstimatedCost = 0.05m,
                    Timestamp = DateTime.UtcNow
                }
            };

            _mockChatUsageService.Setup(x => x.GetFilteredLogsAsync(
                    It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(),
                    It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(logs);

            // Act
            var result = await _controller.GetFilteredLogs(null, null, null, null, null, 1, 20);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<List<ChatUsageLogResponse>>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<ChatUsageLogResponse>>>(okResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Usage logs retrieved successfully", apiResponse.Message);
            Assert.Equal(logs, apiResponse.Data);
        }

        [Fact]
        public async Task GetModelStats_WithValidId_ReturnsSuccessResponse()
        {
            // Arrange
            var stats = new ModelUsageStatResponse
            {
                ModelId = 1,
                ModelName = "gpt-4",
                MessagesCount = 50,
                InputTokens = 25000,
                OutputTokens = 15000,
                TotalTokens = 40000,
                EstimatedCost = 5.25m
            };

            _mockChatUsageService.Setup(x => x.GetModelStatsAsync(1))
                .ReturnsAsync(stats);

            // Act
            var result = await _controller.GetModelStats(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ModelUsageStatResponse>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ModelUsageStatResponse>>(okResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Model usage statistics retrieved successfully", apiResponse.Message);
            Assert.Equal(stats, apiResponse.Data);
        }

        [Fact]
        public async Task GetModelStats_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockChatUsageService.Setup(x => x.GetModelStatsAsync(999))
                .ReturnsAsync((ModelUsageStatResponse)null);

            // Act
            var result = await _controller.GetModelStats(999);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ModelUsageStatResponse>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ModelUsageStatResponse>>(notFoundResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Equal("No usage statistics found for model with ID 999", apiResponse.Message);
        }

        [Fact]
        public async Task GetProviderStats_WithValidId_ReturnsSuccessResponse()
        {
            // Arrange
            var stats = new ProviderUsageStatResponse
            {
                ProviderId = 1,
                ProviderName = "OpenAI",
                MessagesCount = 75,
                TotalTokens = 60000,
                EstimatedCost = 8.75m
            };

            _mockChatUsageService.Setup(x => x.GetProviderStatsAsync(1))
                .ReturnsAsync(stats);

            // Act
            var result = await _controller.GetProviderStats(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ProviderUsageStatResponse>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderUsageStatResponse>>(okResult.Value);

            Assert.True(apiResponse.Success);
            Assert.Equal("Provider usage statistics retrieved successfully", apiResponse.Message);
            Assert.Equal(stats, apiResponse.Data);
        }

        [Fact]
        public async Task GetProviderStats_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockChatUsageService.Setup(x => x.GetProviderStatsAsync(999))
                .ReturnsAsync((ProviderUsageStatResponse)null);

            // Act
            var result = await _controller.GetProviderStats(999);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ProviderUsageStatResponse>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ProviderUsageStatResponse>>(notFoundResult.Value);

            Assert.False(apiResponse.Success);
            Assert.Equal("No usage statistics found for provider with ID 999", apiResponse.Message);
        }
    }
}
