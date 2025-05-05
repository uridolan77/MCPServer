using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Chat;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;
using Message = MCPServer.Core.Models.Message;
using SessionContext = MCPServer.Core.Models.SessionContext;

namespace MCPServer.API.Features.Chat.Controllers
{
    /// <summary>
    /// Controller for chat playground functionality
    /// </summary>
    [ApiController]
    [Route("api/chat")]
    [Authorize(AuthenticationSchemes = "Test,Bearer")]
    public class ChatController : ApiControllerBase
    {
        private readonly ILlmService _llmService;
        private readonly ISessionService _sessionService;
        private readonly IChatUsageService _chatUsageService;
        private readonly IModelService _modelService;
        private readonly IChatStreamingService _chatStreamingService;
        private readonly IDbContextFactory<McpServerDbContext> _dbContextFactory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenManager _tokenManager;

        // Reusable JsonSerializerOptions instance
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ChatController(
            ILogger<ChatController> logger,
            ILlmService llmService,
            ISessionService sessionService,
            IChatUsageService chatUsageService,
            IModelService modelService,
            IChatStreamingService chatStreamingService,
            IDbContextFactory<McpServerDbContext> dbContextFactory,
            IUnitOfWork unitOfWork,
            ITokenManager tokenManager)
            : base(logger)
        {
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _chatUsageService = chatUsageService ?? throw new ArgumentNullException(nameof(chatUsageService));
            _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
            _chatStreamingService = chatStreamingService ?? throw new ArgumentNullException(nameof(chatStreamingService));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        }

        /// <summary>
        /// Gets available models for the chat playground
        /// </summary>
        [HttpGet("models")]
        public async Task<ActionResult<ApiResponse<List<LlmModel>>>> GetAvailableModels()
        {
            try
            {
                _logger.LogInformation("Getting available models for chat playground");

                // Use the ModelService to get available models
                var models = await _modelService.GetAvailableModelsAsync();
                _logger.LogInformation("Found {Count} models using ModelService", models.Count);

                // Ensure all models have their provider information loaded
                foreach (var model in models)
                {
                    if (model.Provider == null && model.ProviderId > 0)
                    {
                        var provider = await _modelService.GetProviderForModelAsync(model);
                        if (provider != null)
                        {
                            _logger.LogInformation("Loaded provider {ProviderName} for model {ModelName}", provider.Name, model.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to load provider for model {ModelName} (ID: {ModelId})", model.Name, model.Id);
                        }
                    }
                }

                // Filter out models with missing provider information
                var validModels = models.Where(m => m.Provider != null).ToList();
                if (validModels.Count < models.Count)
                {
                    _logger.LogWarning("{Count} models were filtered out due to missing provider information", models.Count - validModels.Count);
                }

                return SuccessResponse(validModels);
            }
            catch (Exception ex)
            {
                return ErrorResponse<List<LlmModel>>("Error getting available models", ex);
            }
        }

        /// <summary>
        /// Sends a message to the LLM service
        /// </summary>
        [HttpPost("send")]
        public async Task<ActionResult<ApiResponse<ChatResponse>>> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequestResponse<ChatResponse>("Message is required");
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    // Generate a new session ID if not provided
                    request.SessionId = Guid.NewGuid().ToString();
                    _logger.LogInformation("Generated new session ID: {SessionId}", request.SessionId);
                }

                // Get session history using the SessionService
                List<Message> sessionHistory = request.History ?? await _sessionService.GetSessionHistoryAsync(request.SessionId);

                // If a system prompt is provided and history is empty or doesn't start with a system message,
                // add it as the first message in the context
                if (!string.IsNullOrEmpty(request.SystemPrompt))
                {
                    if (sessionHistory.Count == 0 || sessionHistory[0].Role != "system")
                    {
                        _logger.LogInformation("Adding system prompt to session {SessionId}: '{SystemPrompt}'", request.SessionId, request.SystemPrompt);
                        var systemMessage = new Message
                        {
                            Role = "system",
                            Content = request.SystemPrompt,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        // Add system message at the beginning of the history
                        sessionHistory.Insert(0, systemMessage);
                    }
                    else if (sessionHistory[0].Role == "system" && sessionHistory[0].Content != request.SystemPrompt)
                    {
                        // Update the existing system message if it changed
                        _logger.LogInformation("Updating system prompt for session {SessionId} from '{OldPrompt}' to '{NewPrompt}'", 
                            request.SessionId, sessionHistory[0].Content, request.SystemPrompt);
                        sessionHistory[0].Content = request.SystemPrompt;
                        sessionHistory[0].Timestamp = DateTime.UtcNow;
                    }
                }

                // Create a session context from the chat history
                var context = new SessionContext
                {
                    SessionId = request.SessionId,
                    Messages = sessionHistory
                };

                // Get start time for performance tracking
                var startTime = DateTime.UtcNow;

                // Get the model using the ModelService
                LlmModel? model = null;
                if (request.ModelId.HasValue)
                {
                    model = await _modelService.GetModelByIdAsync(request.ModelId.Value);
                    if (model != null)
                    {
                        _logger.LogInformation("Model requested: {ModelId} - {ModelName}", request.ModelId.Value, model.Name);
                    }
                }

                // Send the message to the LLM service
                string? specificModelId = model?.ModelId;
                var response = await _llmService.SendWithContextAsync(request.Message, context, specificModelId);

                // Add the message and response to the history
                sessionHistory.Add(new Message {
                    Role = "user",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow
                });
                sessionHistory.Add(new Message {
                    Role = "assistant",
                    Content = response,
                    Timestamp = DateTime.UtcNow
                });

                // Calculate duration
                var duration = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

                // Save the updated session data using the SessionService
                await _sessionService.SaveSessionDataAsync(request.SessionId, sessionHistory);

                // Log the usage using the ChatUsageService
                string? username = User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
                await _chatUsageService.LogChatUsageAsync(
                    request.SessionId,
                    request.Message,
                    response,
                    model,
                    duration,
                    true,
                    null,
                    sessionHistory,
                    username);

                var chatResponse = new ChatResponse
                {
                    Message = response,
                    SessionId = request.SessionId
                };

                return SuccessResponse(chatResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message: {Message}", request.Message);

                // Log the failed attempt
                string? username = User?.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
                await _chatUsageService.LogChatUsageAsync(
                    request.SessionId,
                    request.Message,
                    "Error: " + ex.Message,
                    null,
                    0,
                    false,
                    ex.Message,
                    null,
                    username);

                return ErrorResponse<ChatResponse>("An error occurred while processing your message", ex);
            }
        }

        /// <summary>
        /// Initiates a streaming request
        /// </summary>
        [HttpPost("stream")]
        public async Task<ActionResult<ApiResponse<bool>>> StreamMessage([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Message))
                {
                    return BadRequestResponse<bool>("Message is required");
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequestResponse<bool>("SessionId is required");
                }

                // Store the request for processing when the client connects with EventSource
                await _chatStreamingService.StoreStreamRequestAsync(request.SessionId, request);

                _logger.LogInformation("Stored streaming request for session {SessionId}: {Message}",
                    request.SessionId, request.Message);

                return SuccessResponse(true, "Streaming request stored successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>("Error storing streaming request", ex);
            }
        }

        /// <summary>
        /// Streams a response from the LLM service
        /// </summary>
        [HttpGet("stream")]
        public async Task StreamResponse([FromQuery] string sessionId, [FromQuery] string token)
        {
            // Set up Server-Sent Events
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

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
                        sessionId = sessionId
                    };

                    await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorData, _jsonOptions)}\n\n");
                    await Response.Body.FlushAsync();
                    return;
                }

                _logger.LogInformation("Processing stream request for session {SessionId}: {Message}",
                    sessionId, chatRequest.Message);

                // Get session history using SessionService
                List<Message> sessionHistory = chatRequest.History ?? await _sessionService.GetSessionHistoryAsync(sessionId);

                // Handle system prompt for streaming requests
                if (!string.IsNullOrEmpty(chatRequest.SystemPrompt))
                {
                    if (sessionHistory.Count == 0 || sessionHistory[0].Role != "system")
                    {
                        _logger.LogInformation("Adding system prompt to streaming session {SessionId}", sessionId);
                        var systemMessage = new Message
                        {
                            Role = "system",
                            Content = chatRequest.SystemPrompt,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        // Add system message at the beginning of the history
                        sessionHistory.Insert(0, systemMessage);
                    }
                    else if (sessionHistory[0].Role == "system" && sessionHistory[0].Content != chatRequest.SystemPrompt)
                    {
                        // Update the existing system message if it changed
                        _logger.LogInformation("Updating system prompt for streaming session {SessionId}", sessionId);
                        sessionHistory[0].Content = chatRequest.SystemPrompt;
                        sessionHistory[0].Timestamp = DateTime.UtcNow;
                    }
                }

                // Get start time for performance tracking
                var startTime = DateTime.UtcNow;

                // Get the model using ModelService
                LlmModel? model = null;
                if (chatRequest.ModelId.HasValue)
                {
                    model = await _modelService.GetModelByIdAsync(chatRequest.ModelId.Value);
                    if (model != null)
                    {
                        _logger.LogInformation("Using requested model: {ModelName} ({ModelId})",
                            model.Name, model.ModelId);
                    }
                }

                // Add the user's new message to history
                sessionHistory.Add(new Message {
                    Role = "user",
                    Content = chatRequest.Message,
                    Timestamp = DateTime.UtcNow
                });

                // Process the stream using ChatStreamingService
                await _chatStreamingService.ProcessStreamAsync(
                    sessionId,
                    chatRequest.Message,
                    sessionHistory,
                    model,
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

                        await Response.WriteAsync($"data: {JsonSerializer.Serialize(eventData, _jsonOptions)}\n\n");
                        await Response.Body.FlushAsync();

                        // If this is the final chunk, save session and log the usage
                        if (isComplete)
                        {
                            // Calculate duration
                            var duration = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                            string fullResponse = chunk; // In this case, chunk is the full response

                            // Add the assistant's response to the history
                            sessionHistory.Add(new Message {
                                Role = "assistant",
                                Content = fullResponse,
                                Timestamp = DateTime.UtcNow
                            });

                            try {
                                // Create deep copies of the data we need to persist in the background
                                var sessionIdCopy = sessionId;
                                var requestMessageCopy = chatRequest.Message;
                                var responseCopy = fullResponse;
                                var durationCopy = duration;

                                // Create a deep copy of the session history to avoid concurrent modification issues
                                var messageHistoryCopy = sessionHistory.Select(m => new Message
                                {
                                    Role = m.Role,
                                    Content = m.Content,
                                    Timestamp = m.Timestamp
                                }).ToList();

                                // Make a copy of the model data we need
                                LlmModel? modelCopy = null;
                                if (model != null)
                                {
                                    modelCopy = new LlmModel
                                    {
                                        Id = model.Id,
                                        Name = model.Name,
                                        ModelId = model.ModelId,
                                        ProviderId = model.ProviderId,
                                        CostPer1KInputTokens = model.CostPer1KInputTokens,
                                        CostPer1KOutputTokens = model.CostPer1KOutputTokens
                                    };

                                    if (model.Provider != null)
                                    {
                                        modelCopy.Provider = new LlmProvider
                                        {
                                            Id = model.Provider.Id,
                                            Name = model.Provider.Name
                                        };
                                    }
                                }

                                // Use Task.Run to not block the response completion
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        // Get username if available
                                        string? username = null;

                                        // Use our modified services that handle ObjectDisposedException internally
                                        await _sessionService.SaveSessionDataAsync(sessionIdCopy, messageHistoryCopy);

                                        await _chatUsageService.LogChatUsageAsync(
                                            sessionIdCopy,
                                            requestMessageCopy,
                                            responseCopy,
                                            modelCopy,
                                            durationCopy,
                                            true,
                                            null,
                                            messageHistoryCopy,
                                            username);

                                        _logger.LogInformation("Stream chat session saved and usage logged for session {SessionId}", sessionIdCopy);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Failed to save session data or log stream chat usage");
                                    }
                                });
                            }
                            catch (Exception ex) {
                                _logger.LogError(ex, "Failed to start background task for session {SessionId}", sessionId);
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming response for session {SessionId}", sessionId);

                var errorData = new
                {
                    chunk = $"An error occurred while streaming the response: {ex.Message}",
                    isComplete = true,
                    sessionId = sessionId
                };

                await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorData, _jsonOptions)}\n\n");
                await Response.Body.FlushAsync();
            }
        }
    }
}
