using System;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using MCPServer.Core.Services.Llm;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    // Adapter class that implements ILlmService by forwarding to the new implementation
    public class LlmService : ILlmService
    {
        private readonly ILogger<LlmService> _logger;
        private readonly ILlmProviderService _providerService;
        private readonly ICredentialService _credentialService;

        public LlmService(
            ILogger<LlmService> logger,
            ILlmProviderService providerService,
            ICredentialService credentialService)
        {
            _logger = logger;
            _providerService = providerService;
            _credentialService = credentialService;
        }

        public Task<string> SendWithContextAsync(string userInput, SessionContext context, string? specificModelId = null)
        {
            // Implement forwarding to the new service
            _logger.LogInformation("SendWithContextAsync called with user input: {InputLength} chars", userInput?.Length ?? 0);
            // Return placeholder response for now
            return Task.FromResult($"Response to: {userInput}");
        }

        public Task<LlmResponse> SendRequestAsync(LlmRequest request)
        {
            // Implement forwarding to the new service
            _logger.LogInformation("SendRequestAsync called with model: {Model}", request?.Model ?? "null");
            
            // Return placeholder response for now
            var response = new LlmResponse
            {
                Id = "resp_" + Guid.NewGuid().ToString(),
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = request?.Model ?? "unknown",
                Choices = new System.Collections.Generic.List<LlmChoice>
                {
                    new LlmChoice
                    {
                        Index = 0,
                        Message = new LlmResponseMessage
                        {
                            Role = "assistant",
                            Content = $"Response to request with model: {request?.Model ?? "unknown"}"
                        },
                        FinishReason = "stop"
                    }
                },
                Usage = new LlmUsage
                {
                    PromptTokens = 10,
                    CompletionTokens = 20,
                    TotalTokens = 30
                }
            };
            
            return Task.FromResult(response);
        }

        public async Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived)
        {
            // Implement forwarding to the new service
            _logger.LogInformation("StreamResponseAsync called with model: {Model}", request?.Model ?? "null");
            
            // Call onChunkReceived with a placeholder message
            await onChunkReceived("This is a ", false);
            await Task.Delay(100); // Simulate some delay
            await onChunkReceived("placeholder ", false);
            await Task.Delay(100); // Simulate some delay
            await onChunkReceived("streaming response", true);
        }
    }
}