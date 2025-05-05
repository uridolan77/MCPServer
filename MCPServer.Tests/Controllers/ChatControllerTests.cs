using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.Chat.Controllers;
using MCPServer.API.Features.Chat.Models;
using MCPServer.Core.Features.Chat.Services.Interfaces;
using MCPServer.Core.Features.Models.Services.Interfaces;
using MCPServer.Core.Features.Sessions.Services.Interfaces;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Chat;
using MCPServer.Core.Models.Llm;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class ChatControllerTests
    {
        private readonly Mock<IChatStreamingService> _mockChatStreamingService;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IModelService> _mockModelService;
        private readonly Mock<ILogger<ChatController>> _mockLogger;
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _mockChatStreamingService = new Mock<IChatStreamingService>();
            _mockSessionService = new Mock<ISessionService>();
            _mockModelService = new Mock<IModelService>();
            _mockLogger = new Mock<ILogger<ChatController>>();
            
            _controller = new ChatController(
                _mockChatStreamingService.Object,
                _mockSessionService.Object,
                _mockModelService.Object,
                _mockLogger.Object);
            
            // Setup controller context with user claims
            var httpContext = new DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(
                    new System.Security.Claims.Claim[] {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "testuser")
                    }, "TestAuthentication"));
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task SendMessage_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new ChatRequest
            {
                SessionId = "test-session",
                Message = "Hello, world!",
                ModelId = 1
            };

            var model = new LlmModel
            {
                Id = 1,
                Name = "Test Model",
                ModelId = "gpt-3.5-turbo",
                ProviderId = 1,
                IsEnabled = true
            };

            var sessionHistory = new List<Message>
            {
                new Message { Role = "system", Content = "You are a helpful assistant." }
            };

            var expectedResponse = "Hello! How can I assist you today?";

            _mockModelService.Setup(x => x.GetModelByIdAsync(request.ModelId))
                .ReturnsAsync(model);

            _mockSessionService.Setup(x => x.GetSessionHistoryAsync(request.SessionId))
                .ReturnsAsync(sessionHistory);

            _mockChatStreamingService.Setup(x => x.ProcessStreamAsync(
                    request.SessionId,
                    request.Message,
                    sessionHistory,
                    model,
                    It.IsAny<double>(),
                    It.IsAny<int>(),
                    It.IsAny<Func<string, bool, Task>>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ChatResponse>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ChatResponse>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Message processed successfully", apiResponse.Message);
            Assert.Equal(expectedResponse, apiResponse.Data.Response);
            Assert.Equal(request.SessionId, apiResponse.Data.SessionId);
        }

        [Fact]
        public async Task SendMessage_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SendMessage(null);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ChatResponse>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ChatResponse>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Request cannot be null", apiResponse.Message);
        }

        [Fact]
        public async Task SendMessage_WithEmptyMessage_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest
            {
                SessionId = "test-session",
                Message = "",
                ModelId = 1
            };

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ChatResponse>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ChatResponse>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Message cannot be empty", apiResponse.Message);
        }

        [Fact]
        public async Task SendMessage_WithInvalidModel_ReturnsNotFound()
        {
            // Arrange
            var request = new ChatRequest
            {
                SessionId = "test-session",
                Message = "Hello, world!",
                ModelId = 999 // Non-existent model ID
            };

            _mockModelService.Setup(x => x.GetModelByIdAsync(request.ModelId))
                .ReturnsAsync((LlmModel)null);

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<ChatResponse>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<ChatResponse>>(notFoundResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Model not found", apiResponse.Message);
        }

        [Fact]
        public async Task GetSessionHistory_WithValidSessionId_ReturnsSuccessResponse()
        {
            // Arrange
            var sessionId = "test-session";
            var sessionHistory = new List<Message>
            {
                new Message { Role = "system", Content = "You are a helpful assistant." },
                new Message { Role = "user", Content = "Hello" },
                new Message { Role = "assistant", Content = "Hi there! How can I help you?" }
            };

            _mockSessionService.Setup(x => x.GetSessionHistoryAsync(sessionId))
                .ReturnsAsync(sessionHistory);

            // Act
            var result = await _controller.GetSessionHistory(sessionId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<List<Message>>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<Message>>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Session history retrieved successfully", apiResponse.Message);
            Assert.Equal(sessionHistory, apiResponse.Data);
        }

        [Fact]
        public async Task GetSessionHistory_WithEmptySessionId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetSessionHistory("");

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<List<Message>>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<List<Message>>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Session ID cannot be empty", apiResponse.Message);
        }

        [Fact]
        public async Task DeleteSession_WithValidSessionId_ReturnsSuccessResponse()
        {
            // Arrange
            var sessionId = "test-session";

            _mockSessionService.Setup(x => x.DeleteSessionAsync(sessionId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteSession(sessionId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Session deleted successfully", apiResponse.Message);
            Assert.True(apiResponse.Data);
        }

        [Fact]
        public async Task DeleteSession_WithInvalidSessionId_ReturnsNotFound()
        {
            // Arrange
            var sessionId = "non-existent-session";

            _mockSessionService.Setup(x => x.DeleteSessionAsync(sessionId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteSession(sessionId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(notFoundResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Session not found", apiResponse.Message);
            Assert.False(apiResponse.Data);
        }
    }
}
