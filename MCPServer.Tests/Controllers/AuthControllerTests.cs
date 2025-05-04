using System;
using System.Threading.Tasks;
using MCPServer.API.Controllers;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Auth;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _loggerMock = new Mock<ILogger<AuthController>>();
            _userServiceMock = new Mock<IUserService>();
            _controller = new AuthController(_userServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Register_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123!"
            };

            var response = new AuthResponse
            {
                Username = "testuser",
                Token = "test-token",
                RefreshToken = "test-refresh-token"
            };

            _userServiceMock.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("User registered successfully", apiResponse.Message);
            Assert.Equal(response, apiResponse.Data);
        }

        [Fact]
        public async Task Register_WithExistingUsername_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Username = "existinguser",
                Email = "existing@example.com",
                Password = "Password123!"
            };

            _userServiceMock.Setup(s => s.RegisterAsync(request))
                .ReturnsAsync((AuthResponse)null);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Username or email already exists", apiResponse.Message);
            Assert.Null(apiResponse.Data);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "testuser",
                Password = "Password123!"
            };

            var response = new AuthResponse
            {
                Username = "testuser",
                Token = "test-token",
                RefreshToken = "test-refresh-token"
            };

            _userServiceMock.Setup(s => s.AuthenticateAsync(request))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Login successful", apiResponse.Message);
            Assert.Equal(response, apiResponse.Data);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest
            {
                Username = "wronguser",
                Password = "WrongPassword123!"
            };

            _userServiceMock.Setup(s => s.AuthenticateAsync(request))
                .ReturnsAsync((AuthResponse)null);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(unauthorizedResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Invalid username or password", apiResponse.Message);
            Assert.Null(apiResponse.Data);
        }

        [Fact]
        public async Task RefreshToken_WithValidToken_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid-refresh-token"
            };

            var response = new AuthResponse
            {
                Username = "testuser",
                Token = "new-token",
                RefreshToken = "new-refresh-token"
            };

            _userServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Token refreshed successfully", apiResponse.Message);
            Assert.Equal(response, apiResponse.Data);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "invalid-refresh-token"
            };

            _userServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken))
                .ReturnsAsync((AuthResponse)null);

            // Act
            var result = await _controller.RefreshToken(request);

            // Assert
            var unauthorizedResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(401, unauthorizedResult.StatusCode);
            
            var apiResponse = Assert.IsType<ApiResponse<AuthResponse>>(unauthorizedResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Invalid refresh token", apiResponse.Message);
            Assert.Null(apiResponse.Data);
        }

        [Fact]
        public async Task RevokeToken_WithValidToken_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "valid-refresh-token"
            };

            _userServiceMock.Setup(s => s.RevokeTokenAsync(request.RefreshToken))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Token revoked successfully", apiResponse.Message);
            Assert.True(apiResponse.Data);
        }

        [Fact]
        public async Task RevokeToken_WithInvalidToken_ReturnsNotFound()
        {
            // Arrange
            var request = new RefreshTokenRequest
            {
                RefreshToken = "invalid-refresh-token"
            };

            _userServiceMock.Setup(s => s.RevokeTokenAsync(request.RefreshToken))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RevokeToken(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(notFoundResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Token not found", apiResponse.Message);
            Assert.Null(apiResponse.Data);
        }
    }
}
