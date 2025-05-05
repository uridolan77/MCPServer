using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using Microsoft.Extensions.Logging;
using MCPServer.Core.Features.Sessions.Services.Interfaces;

namespace MCPServer.Core.Features.Sessions.Services
{
    /// <summary>
    /// Service implementation for managing session contexts
    /// </summary>
    public class SessionContextService : ISessionContextService
    {
        private readonly ISessionService _sessionService;
        private readonly ILogger<SessionContextService> _logger;

        public SessionContextService(
            ISessionService sessionService,
            ILogger<SessionContextService> logger)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<SessionContext> GetOrCreateSessionContextAsync(string sessionId)
        {
            try
            {
                var messages = await _sessionService.GetSessionHistoryAsync(sessionId);
                
                return new SessionContext
                {
                    SessionId = sessionId,
                    Messages = messages,
                    LastUpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating session context for session {SessionId}", sessionId);
                
                // Return an empty context if there was an error
                return new SessionContext
                {
                    SessionId = sessionId,
                    Messages = new List<Message>(),
                    LastUpdatedAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc />
        public async Task AddUserMessageAsync(string sessionId, string message)
        {
            try
            {
                var messages = await _sessionService.GetSessionHistoryAsync(sessionId);
                
                messages.Add(new Message
                {
                    Role = "user",
                    Content = message,
                    Timestamp = DateTime.UtcNow
                });
                
                await _sessionService.SaveSessionDataAsync(sessionId, messages);
                
                _logger.LogDebug("Added user message to session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user message to session {SessionId}", sessionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AddAssistantMessageAsync(string sessionId, string message)
        {
            try
            {
                var messages = await _sessionService.GetSessionHistoryAsync(sessionId);
                
                messages.Add(new Message
                {
                    Role = "assistant",
                    Content = message,
                    Timestamp = DateTime.UtcNow
                });
                
                await _sessionService.SaveSessionDataAsync(sessionId, messages);
                
                _logger.LogDebug("Added assistant message to session {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding assistant message to session {SessionId}", sessionId);
                throw;
            }
        }
    }
}




