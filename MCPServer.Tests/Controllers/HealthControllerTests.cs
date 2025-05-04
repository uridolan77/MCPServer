using System;
using MCPServer.API.Controllers;
using MCPServer.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class HealthControllerTests
    {
        private readonly Mock<ILogger<HealthController>> _loggerMock;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _loggerMock = new Mock<ILogger<HealthController>>();
            _controller = new HealthController(_loggerMock.Object);
        }

        [Fact]
        public void Get_ReturnsSuccessResponse()
        {
            // Act
            var result = _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Equal("API is running", response.Message);
            
            var data = Assert.IsType<dynamic>(response.Data);
            Assert.Equal("healthy", data.status);
        }
    }
}
