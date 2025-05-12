using System;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace MCPServer.Core.Services.Llm
{
    // Adapter class to maintain compatibility with existing code
    // Implements our local ILlmClient interface and forwards requests to the new implementation
    public class OpenAiClient : ILlmClient
    {
        private readonly Features.Llm.Services.Llm.OpenAiClient _innerClient;

        public OpenAiClient(
            string apiKey,
            string endpoint,
            string modelId,
            ILogger logger,
            HttpClient httpClient)
        {
            // Create a properly typed logger for the inner client
            // This adapts a generic ILogger to the typed ILogger<T> expected by the new implementation
            var typedLogger = (ILogger<Features.Llm.Services.Llm.OpenAiClient>)
                (logger is ILogger<Features.Llm.Services.Llm.OpenAiClient> typed ? typed : 
                new TypedLoggerAdapter<Features.Llm.Services.Llm.OpenAiClient>(logger));
            
            _innerClient = new Features.Llm.Services.Llm.OpenAiClient(
                apiKey: apiKey,
                endpoint: endpoint,
                modelId: modelId,
                logger: typedLogger,
                httpClient: httpClient);
        }

        // Implement the required methods by forwarding to the inner client
        public Task<LlmResponse> SendRequestAsync(LlmRequest request)
        {
            return _innerClient.SendRequestAsync(request);
        }

        public Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived)
        {
            return _innerClient.StreamResponseAsync(request, onChunkReceived);
        }
    }
}