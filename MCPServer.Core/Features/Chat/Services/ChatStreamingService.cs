using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Features.Llm.Services.Interfaces;
using MCPServer.Core.Features.Chat.Services.Interfaces;

namespace MCPServer.Core.Features.Chat.Services
{
    public class ChatStreamingService : IChatStreamingService
    {
        private readonly ILlmService _llmService;
        private readonly ILogger<ChatStreamingService> _logger;

        // Dictionary to store pending stream requests by session ID
        private static readonly Dictionary<string, object> _pendingStreamRequests = [];

        public ChatStreamingService(ILlmService llmService, ILogger<ChatStreamingService> logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        public async Task StoreStreamRequestAsync(string sessionId, object request)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
            }

            lock (_pendingStreamRequests)
            {
                _pendingStreamRequests[sessionId] = request;
            }

            _logger.LogInformation("Stored streaming request for session {SessionId}", sessionId);
            await Task.CompletedTask;
        }

        public Task<object?> GetPendingStreamRequestAsync(string sessionId)
        {
            object? pendingRequest = null;

            lock (_pendingStreamRequests)
            {
                if (_pendingStreamRequests.TryGetValue(sessionId, out var request))
                {
                    pendingRequest = request;
                    _pendingStreamRequests.Remove(sessionId);
                }
            }

            if (pendingRequest is null)
            {
                _logger.LogWarning("No pending stream request found for session {SessionId}", sessionId);
            }
            else
            {
                _logger.LogInformation("Retrieved pending stream request for session {SessionId}", sessionId);
            }

            return Task.FromResult(pendingRequest);
        }

        public async Task<string> ProcessStreamAsync(
            string sessionId,
            string message,
            List<Message> sessionHistory,
            LlmModel? model,
            double temperature = 0.7,
            int maxTokens = 2000,
            Func<string, bool, Task>? onChunkReceived = null)
        {
            _logger.LogInformation("Processing stream for session {SessionId} with {HistoryCount} previous messages",
                sessionId, sessionHistory?.Count ?? 0);

            // Create a session context from chat history if provided
            var context = new SessionContext
            {
                SessionId = sessionId,
                Messages = sessionHistory ?? []
            };

            // If a specific model is requested, use its ModelId
            string modelId = "gpt-3.5-turbo"; // Default model

            if (model != null)
            {
                modelId = model.ModelId;
                _logger.LogInformation("Using requested model: {ModelName} ({ModelId})",
                    model.Name, modelId);
            }

            // Create request with user's message
            var llmRequest = new LlmRequest
            {
                Model = modelId,
                Messages = [.. context.Messages.Select(m => new LlmMessage
                {
                    Role = m.Role,
                    Content = m.Content
                })],
                Stream = true,
                Temperature = temperature,
                Max_tokens = maxTokens
            };

            // Add the user's message to the LLM request
            llmRequest.Messages.Add(new LlmMessage
            {
                Role = "user",
                Content = message
            });

            _logger.LogInformation("Sending LLM request with {MessageCount} messages",
                llmRequest.Messages.Count);

            // Use a StringBuilder to accumulate the streamed chunks
            var responseBuilder = new StringBuilder();

            // Use the LLM service to stream the response
            await _llmService.StreamResponseAsync(llmRequest, async (chunk, isComplete) =>
            {
                // Accumulate the response
                responseBuilder.Append(chunk);

                // Call the callback function if provided
                if (onChunkReceived != null)
                {
                    await onChunkReceived(chunk, isComplete);
                }
            });

            // Return the complete accumulated response
            return responseBuilder.ToString();
        }

        public Task RemovePendingStreamRequestAsync(string sessionId)
        {
            lock (_pendingStreamRequests)
            {
                if (_pendingStreamRequests.Remove(sessionId))
                {
                    _logger.LogInformation("Removed pending stream request for session {SessionId}", sessionId);
                }
                else
                {
                    _logger.LogWarning("No pending stream request found to remove for session {SessionId}", sessionId);
                }
            }

            return Task.CompletedTask;
        }
    }
}



