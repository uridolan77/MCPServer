using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Chat;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Features.Chat.Services.Interfaces;
using MCPServer.Core.Features.Llm.Services.Interfaces;
using MCPServer.Core.Features.Models.Services.Interfaces;
using MCPServer.Core.Features.Sessions.Services.Interfaces;
using MCPServer.Core.Features.Usage.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Chat.Services
{
    /// <summary>
    /// Service implementation for chat playground functionality
    /// </summary>
    [Obsolete("This service is being replaced by more specialized services. Use ModelService, LlmService, and ChatStreamingService instead.")]
    public class ChatPlaygroundService : IChatPlaygroundService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILlmService _llmService;
        private readonly IModelService _modelService;
        private readonly ISessionContextService _sessionContextService;
        private readonly IChatUsageService _chatUsageService;
        private readonly ILogger<ChatPlaygroundService> _logger;

        public ChatPlaygroundService(
            McpServerDbContext dbContext,
            ILlmService llmService,
            IModelService modelService,
            ISessionContextService sessionContextService,
            IChatUsageService chatUsageService,
            ILogger<ChatPlaygroundService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
            _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
            _sessionContextService = sessionContextService ?? throw new ArgumentNullException(nameof(sessionContextService));
            _chatUsageService = chatUsageService ?? throw new ArgumentNullException(nameof(chatUsageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<List<LlmModel>> GetAvailableModelsAsync()
        {
            _logger.LogInformation("Getting available models for chat playground");

            // Test database connection
            await TestDatabaseConnectionAsync();

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

            return validModels;
        }

        /// <inheritdoc />
        public async Task<ChatResponse> SendMessageAsync(ChatRequest request, string? username = null)
        {
            _logger.LogInformation("Processing chat message for session {SessionId}", request.SessionId);

            var stopwatch = Stopwatch.StartNew();

            // Get the model
            if (!request.ModelId.HasValue)
            {
                throw new ArgumentException("ModelId is required");
            }

            var model = await _modelService.GetModelByIdAsync(request.ModelId.Value);
            if (model == null)
            {
                throw new ArgumentException($"Model with ID {request.ModelId} not found");
            }

            // Get or create session context
            var context = await _sessionContextService.GetOrCreateSessionContextAsync(request.SessionId);

            // Add user message to context
            await _sessionContextService.AddUserMessageAsync(request.SessionId, request.Message);

            // Send message to LLM
            var llmRequest = new LlmRequest
            {
                Model = model.ModelId,
                Messages = context.Messages.Select(m => new MCPServer.Core.Models.LlmMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Temperature = request.Temperature.HasValue ? (double)request.Temperature.Value : 0.7,
                Max_tokens = request.MaxTokens.HasValue ? request.MaxTokens.Value : 2000
            };

            var llmResponse = await _llmService.SendRequestAsync(llmRequest);

            // Extract content from the response
            string responseContent = "";
            if (llmResponse.Choices.Count > 0 && llmResponse.Choices[0].Message != null)
            {
                responseContent = llmResponse.Choices[0].Message.Content ?? string.Empty;
            }

            // Add assistant response to context
            await _sessionContextService.AddAssistantMessageAsync(request.SessionId, responseContent);

            stopwatch.Stop();

            // Log usage
            await _chatUsageService.LogChatUsageAsync(
                request.SessionId,
                request.Message,
                responseContent,
                model,
                (int)stopwatch.ElapsedMilliseconds,
                true,
                null,
                context.Messages,
                username);

            return new ChatResponse
            {
                SessionId = request.SessionId,
                Message = responseContent,
                Model = model,
                ProcessingTime = stopwatch.ElapsedMilliseconds
            };
        }

        /// <inheritdoc />
        public async Task StreamResponseAsync(string sessionId, string message, LlmModel model, Func<string, bool, Task> onChunkReceived, string? username = null)
        {
            _logger.LogInformation("Streaming response for session {SessionId}", sessionId);

            var stopwatch = Stopwatch.StartNew();

            // Get or create session context
            var context = await _sessionContextService.GetOrCreateSessionContextAsync(sessionId);

            // Add user message to context
            await _sessionContextService.AddUserMessageAsync(sessionId, message);

            // Create LLM request
            var llmRequest = new LlmRequest
            {
                Model = model.ModelId,
                Messages = context.Messages.Select(m => new MCPServer.Core.Models.LlmMessage
                {
                    Role = m.Role,
                    Content = m.Content
                }).ToList(),
                Temperature = 0.7, // Default temperature
                Max_tokens = 2000, // Default max tokens
                Stream = true
            };

            // Collect the full response for logging
            var fullResponse = new System.Text.StringBuilder();

            // Stream the response
            await _llmService.StreamResponseAsync(llmRequest, async (chunk, isComplete) =>
            {
                // Append to the full response
                fullResponse.Append(chunk);

                // Forward the chunk to the caller
                await onChunkReceived(chunk, isComplete);

                // If complete, add the full response to the session context
                if (isComplete)
                {
                    var completeResponse = fullResponse.ToString();
                    await _sessionContextService.AddAssistantMessageAsync(sessionId, completeResponse);

                    stopwatch.Stop();

                    // Log usage
                    await _chatUsageService.LogChatUsageAsync(
                        sessionId,
                        message,
                        completeResponse,
                        model,
                        (int)stopwatch.ElapsedMilliseconds,
                        true,
                        null,
                        context.Messages,
                        username);
                }

                // Don't return anything from the async lambda
                return;
            });
        }

        /// <inheritdoc />
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                var canConnect = await _dbContext.Database.CanConnectAsync();
                _logger.LogInformation("Database connection test: {CanConnect}", canConnect);

                if (!canConnect)
                {
                    _logger.LogWarning("Cannot connect to database");
                    throw new InvalidOperationException("Cannot connect to database");
                }

                return true;
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error testing database connection");
                throw new InvalidOperationException("Database connection error", dbEx);
            }
        }
    }
}
