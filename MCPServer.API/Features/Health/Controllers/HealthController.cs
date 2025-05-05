using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Models;
using MCPServer.API.Features.Shared;

namespace MCPServer.API.Features.Health.Controllers
{
    /// <summary>
    /// Controller for health checks
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ApiControllerBase
    {
        public HealthController(ILogger<HealthController> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get API health status
        /// </summary>
        [HttpGet]
        public ActionResult<ApiResponse<object>> Get()
        {
            return SuccessResponse<object>(new { status = "healthy" }, "API is running");
        }
    }
}
