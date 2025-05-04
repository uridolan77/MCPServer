using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Chat;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Controllers
{
    /// <summary>
    /// Legacy controller for chat playground functionality
    /// This controller is being replaced by the ChatController
    /// </summary>
    [ApiController]
    [Route("api/chat-playground")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    [Obsolete("This controller is being replaced by ChatController. Use the new controller instead.")]
    public class ChatPlaygroundController : ApiControllerBase
    {
        private readonly IChatPlaygroundService _chatPlaygroundService;
        private readonly IChatStreamingService _chatStreamingService;

        public ChatPlaygroundController(
            ILogger<ChatPlaygroundController> logger,
            IChatPlaygroundService chatPlaygroundService,
            IChatStreamingService chatStreamingService)
            : base(logger)
        {
            _chatPlaygroundService = chatPlaygroundService ?? throw new ArgumentNullException(nameof(chatPlaygroundService));
            _chatStreamingService = chatStreamingService ?? throw new ArgumentNullException(nameof(chatStreamingService));
        }

        [HttpGet("models")]
        [AllowAnonymous] // Allow anonymous access for debugging
        public async Task<ActionResult<List<LlmModel>>> GetAvailableModels()
        {
            try
            {
                _logger.LogInformation("Getting available models for chat playground");

                var models = await _chatPlaygroundService.GetAvailableModelsAsync();

                return Ok(models);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Database connection error: {Message}", ex.Message);
                return StatusCode(500, new { error = "Database connection error", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available models: {Message}", ex.Message);
                return StatusCode(500, new { error = "An error occurred while retrieving models", details = ex.Message });
            }
        }

        [HttpPost("send")]
        [AllowAnonymous] // Allow anonymous access for debugging
        public async Task<ActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequest("Message is required");
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    // Generate a new session ID if not provided
                    request.SessionId = Guid.NewGuid().ToString();
                    _logger.LogInformation("Generated new session ID: {SessionId}", request.SessionId);
                }

                // Get the current username if authenticated
                string? username = User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;

                // Use the service to process the message
                var response = await _chatPlaygroundService.SendMessageAsync(request, username);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message: {Message}", request?.Message ?? "Unknown");
                return StatusCode(500, "An error occurred while processing your message");
            }
        }

        [HttpPost("stream")]
        [AllowAnonymous] // Allow anonymous access for debugging
        public async Task<ActionResult> StreamMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
            {
                return BadRequest("Message is required");
            }

            if (string.IsNullOrEmpty(request.SessionId))
            {
                return BadRequest("SessionId is required");
            }

            // Store the request for processing when the client connects with EventSource
            await _chatStreamingService.StoreStreamRequestAsync(request.SessionId, request);

            _logger.LogInformation("Stored streaming request for session {SessionId}: {Message}",
                request.SessionId, request.Message);

            return Ok();
        }

        [HttpGet("stream")]
        [AllowAnonymous] // Allow anonymous access for debugging
        public async Task StreamResponse([FromQuery] string sessionId)
        {
            // Set up Server-Sent Events
            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            try
            {
                // Try to get the pending request for this session using ChatStreamingService
                var pendingRequest = await _chatStreamingService.GetPendingStreamRequestAsync(sessionId);
                var chatRequest = pendingRequest as ChatRequest;

                if (chatRequest == null)
                {
                    // No pending request found
                    _logger.LogWarning("No pending stream request found for session {SessionId}", sessionId);
                    var errorData = new
                    {
                        chunk = "No pending request found for this session",
                        isComplete = true,
                        sessionId
                    };

                    await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(errorData)}\n\n");
                    await Response.Body.FlushAsync();
                    return;
                }

                _logger.LogInformation("Processing stream request for session {SessionId}: {Message}",
                    sessionId, chatRequest.Message);

                // The user's message is already in the request

                // Process the stream using ChatStreamingService
                // This handles all the complex streaming logic
                await _chatStreamingService.ProcessStreamAsync(
                    sessionId,
                    chatRequest.Message,
                    chatRequest.History ?? new List<Message>(),
                    null, // We'll let the service resolve the model
                    chatRequest.Temperature ?? 0.7,
                    chatRequest.MaxTokens ?? 2000,
                    async (chunk, isComplete) =>
                    {
                        // Send the chunk to the client
                        var eventData = new
                        {
                            chunk,
                            isComplete,
                            sessionId
                        };

                        await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(eventData)}\n\n");
                        await Response.Body.FlushAsync();
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming response for session {SessionId}", sessionId);

                var errorData = new
                {
                    chunk = $"An error occurred while streaming the response: {ex.Message}",
                    isComplete = true,
                    sessionId
                };

                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(errorData)}\n\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}
