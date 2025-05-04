using Microsoft.AspNetCore.Mvc;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using System.Threading.Tasks;

namespace MCPServer.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LlmController : ControllerBase
    {
        private readonly ILlmService _llmService;

        public LlmController(ILlmService llmService)
        {
            _llmService = llmService;
        }

        [HttpPost("send")]
        public async Task<ActionResult<LlmResponse>> SendRequest([FromBody] LlmRequest request)
        {
            try
            {
                var response = await _llmService.SendRequestAsync(request);
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("context")]
        public async Task<ActionResult<string>> SendWithContext([FromBody] ContextRequest request)
        {
            try
            {
                var context = new SessionContext
                {
                    SessionId = request.SessionId,
                    Messages = request.Messages
                };

                var response = await _llmService.SendWithContextAsync(request.UserInput, context);
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }

    public class ContextRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserInput { get; set; } = string.Empty;
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
