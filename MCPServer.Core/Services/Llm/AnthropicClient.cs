using System;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Features.Llm.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace MCPServer.Core.Services.Llm
{
    // Adapter class to maintain compatibility with existing code
    // Implements both the old namespace's interface (for compatibility)
    // and forwards requests to the new implementation
    public class AnthropicClient : ILlmClient
    {
        private readonly Features.Llm.Services.Llm.AnthropicClient _innerClient;

        public AnthropicClient(
            string apiKey,
            string endpoint,
            string modelId,
            ILogger logger,
            HttpClient httpClient)
        {
            // Create a properly typed logger for the inner client
            // This adapts a generic ILogger to the typed ILogger<T> expected by the new implementation
            var typedLogger = (ILogger<Features.Llm.Services.Llm.AnthropicClient>)
                (logger is ILogger<Features.Llm.Services.Llm.AnthropicClient> typed ? typed : 
                new TypedLoggerAdapter<Features.Llm.Services.Llm.AnthropicClient>(logger));
            
            _innerClient = new Features.Llm.Services.Llm.AnthropicClient(
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

    // Define our own ILlmClient interface for backward compatibility
    public interface ILlmClient
    {
        Task<LlmResponse> SendRequestAsync(LlmRequest request);
        Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived);
    }

    // Helper class to adapt generic ILogger to typed ILogger<T>
    internal class TypedLoggerAdapter<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public TypedLoggerAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}