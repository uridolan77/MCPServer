using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Models;

namespace MCPServer.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ApiControllerBase
    {
        public HealthController(ILogger<HealthController> logger)
            : base(logger)
        {
        }

        [HttpGet]
        public ActionResult<ApiResponse<object>> Get()
        {
            return SuccessResponse<object>(new { status = "healthy" }, "API is running");
        }
    }
}
