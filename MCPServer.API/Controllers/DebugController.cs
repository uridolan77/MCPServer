using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;

        public DebugController(ILogger<DebugController> logger)
        {
            _logger = logger;
        }

        [HttpGet("auth")]
        public IActionResult TestAuth()
        {
            // Check if the request has an authenticated user
            if (User.Identity?.IsAuthenticated == true)
            {
                // Return user authentication information
                return Ok(new
                {
                    isAuthenticated = true,
                    username = User.Identity.Name,
                    claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToArray()
                });
            }
            else
            {
                // User is not authenticated
                return Ok(new
                {
                    isAuthenticated = false,
                    message = "No authenticated user found"
                });
            }
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult TestProtected()
        {
            // This endpoint requires authentication
            return Ok(new
            {
                message = $"Authentication successful for user {User.Identity?.Name}",
                claims = User.Claims.Select(c => new { type = c.Type, value = c.Value }).ToArray()
            });
        }
    }
}
