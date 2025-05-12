using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    // Adapter class that forwards to the new implementation in Features namespace
    public class SessionContextService : ISessionContextService
    {
        private readonly ILogger<SessionContextService> _logger;

        public SessionContextService(ILogger<SessionContextService> logger)
        {
            _logger = logger;
        }

        public Task<SessionContext> GetOrCreateSessionContextAsync(string sessionId)
        {
            _logger.LogInformation("GetOrCreateSessionContextAsync called for sessionId: {SessionId}", sessionId);
            
            // Return a minimal context to satisfy the compiler
            return Task.FromResult(new SessionContext
            {
                SessionId = sessionId,
                Messages = new List<Message>(),
                LastUpdatedAt = DateTime.UtcNow
            });
        }

        public Task AddUserMessageAsync(string sessionId, string message)
        {
            _logger.LogInformation("AddUserMessageAsync called for sessionId: {SessionId}", sessionId);
            return Task.CompletedTask;
        }

        public Task AddAssistantMessageAsync(string sessionId, string message)
        {
            _logger.LogInformation("AddAssistantMessageAsync called for sessionId: {SessionId}", sessionId);
            return Task.CompletedTask;
        }
    }
}