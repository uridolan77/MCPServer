using System;
using System.Threading.Tasks;
using MCPServer.API.Features.Llm.Controllers;
using MCPServer.API.Features.Llm.Models;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class LlmControllerTests
    {
        private readonly Mock<ILlmService> _mockLlmService;
        private readonly Mock<ILogger<LlmController>> _mockLogger;
        private readonly LlmController _controller;

        public LlmControllerTests()
        {
            _mockLlmService = new Mock<ILlmService>();
            _mockLogger = new Mock<ILogger<LlmController>>();
            _controller = new LlmController(_mockLlmService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendRequest_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new LlmRequest
            {
                ModelId = "gpt-3.5-turbo",
                Messages = new System.Collections.Generic.List<LlmMessage>
                {
                    new LlmMessage { Role = "user", Content = "Hello, world!" }
                }
            };

            var response = new LlmResponse
            {
                Content = "Hello! How can I assist you today?",
                ModelId = "gpt-3.5-turbo",
                FinishReason = "stop",
                Usage = new LlmUsage
                {
                    PromptTokens = 10,
                    CompletionTokens = 8,
                    TotalTokens = 18
                }
            };

            _mockLlmService.Setup(x => x.SendRequestAsync(It.IsAny<LlmRequest>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.SendRequest(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<LlmResponse>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<LlmResponse>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Request processed successfully", apiResponse.Message);
            Assert.Equal(response, apiResponse.Data);
        }

        [Fact]
        public async Task SendRequest_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SendRequest(null);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<LlmResponse>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<LlmResponse>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Request cannot be null", apiResponse.Message);
        }

        [Fact]
        public async Task SendRequest_WhenExceptionOccurs_ReturnsErrorResponse()
        {
            // Arrange
            var request = new LlmRequest
            {
                ModelId = "gpt-3.5-turbo",
                Messages = new System.Collections.Generic.List<LlmMessage>
                {
                    new LlmMessage { Role = "user", Content = "Hello, world!" }
                }
            };

            _mockLlmService.Setup(x => x.SendRequestAsync(It.IsAny<LlmRequest>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.SendRequest(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<LlmResponse>>>(result);
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var apiResponse = Assert.IsType<ApiResponse<LlmResponse>>(statusCodeResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Error processing LLM request", apiResponse.Message);
        }

        [Fact]
        public async Task SendWithContext_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new ContextRequest
            {
                SessionId = "test-session",
                UserInput = "Hello, world!",
                Messages = new System.Collections.Generic.List<Message>()
            };

            var response = "Hello! How can I assist you today?";

            _mockLlmService.Setup(x => x.SendWithContextAsync(It.IsAny<string>(), It.IsAny<SessionContext>(), It.IsAny<string>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.SendWithContext(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<string>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Context request processed successfully", apiResponse.Message);
            Assert.Equal(response, apiResponse.Data);
        }

        [Fact]
        public async Task SendWithContext_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SendWithContext(null);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<string>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Request cannot be null", apiResponse.Message);
        }

        [Fact]
        public async Task SendWithContext_WithEmptyUserInput_ReturnsBadRequest()
        {
            // Arrange
            var request = new ContextRequest
            {
                SessionId = "test-session",
                UserInput = "",
                Messages = new System.Collections.Generic.List<Message>()
            };

            // Act
            var result = await _controller.SendWithContext(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<string>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("User input is required", apiResponse.Message);
        }

        [Fact]
        public async Task SendWithContext_WithEmptySessionId_ReturnsBadRequest()
        {
            // Arrange
            var request = new ContextRequest
            {
                SessionId = "",
                UserInput = "Hello, world!",
                Messages = new System.Collections.Generic.List<Message>()
            };

            // Act
            var result = await _controller.SendWithContext(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<string>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Session ID is required", apiResponse.Message);
        }
    }
}
