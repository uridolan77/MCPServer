using System;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Auth;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;
using MCPServer.API.Features.Auth.Models;

namespace MCPServer.API.Features.Auth.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(
            IUserService userService,
            ILogger<AuthController> logger)
            : base(logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var response = await _userService.RegisterAsync(request);

                if (response == null)
                {
                    return BadRequestResponse<AuthResponse>("Username or email already exists");
                }

                return SuccessResponse(response, "User registered successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<AuthResponse>("Error registering user", ex);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _userService.AuthenticateAsync(request);

                if (response == null)
                {
                    return StatusCode(401, ApiResponse<AuthResponse>.ErrorResponse("Invalid username or password"));
                }

                return SuccessResponse(response, "Login successful");
            }
            catch (Exception ex)
            {
                return ErrorResponse<AuthResponse>("Error logging in user", ex);
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var response = await _userService.RefreshTokenAsync(request.RefreshToken);

                if (response == null)
                {
                    return StatusCode(401, ApiResponse<AuthResponse>.ErrorResponse("Invalid refresh token"));
                }

                return SuccessResponse(response, "Token refreshed successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<AuthResponse>("Error refreshing token", ex);
            }
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _userService.RevokeTokenAsync(request.RefreshToken);

                if (!result)
                {
                    return NotFoundResponse<bool>("Token not found");
                }

                return SuccessResponse(true, "Token revoked successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>("Error revoking token", ex);
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<User>>> GetCurrentUser()
        {
            try
            {
                var username = User.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                {
                    return StatusCode(401, ApiResponse<User>.ErrorResponse("User not authenticated"));
                }

                var user = await _userService.GetUserByUsernameAsync(username);

                if (user == null)
                {
                    return NotFoundResponse<User>("User not found");
                }

                // Don't return the password hash
                user.PasswordHash = string.Empty;

                return SuccessResponse(user, "User retrieved successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<User>("Error getting current user", ex);
            }
        }
    }
}


