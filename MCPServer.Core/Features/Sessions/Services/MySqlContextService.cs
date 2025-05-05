using System;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MCPServer.Core.Features.Auth.Services.Interfaces;
using MCPServer.Core.Features.Sessions.Services.Interfaces;

namespace MCPServer.Core.Features.Sessions.Services
{
    public class MySqlContextService : IContextService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<MySqlContextService> _logger;
        private readonly AppSettings _appSettings;
        private readonly ITokenManager _tokenManager;

        public MySqlContextService(
            McpServerDbContext dbContext,
            IOptions<AppSettings> appSettings,
            ILogger<MySqlContextService> logger,
            ITokenManager tokenManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _appSettings = appSettings.Value;
            _tokenManager = tokenManager;
        }

        public async Task<SessionContext> GetSessionContextAsync(string sessionId)
        {
            try
            {
                var sessionData = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (sessionData == null)
                {
                    _logger.LogInformation("Creating new session context for {SessionId}", sessionId);
                    return new SessionContext { SessionId = sessionId };
                }

                var context = JsonSerializer.Deserialize<SessionContext>(sessionData.Data);

                if (context == null)
                {
                    _logger.LogWarning("Failed to deserialize context for {SessionId}, creating new", sessionId);
                    return new SessionContext { SessionId = sessionId };
                }

                // Update expiry
                sessionData.LastUpdatedAt = DateTime.UtcNow;
                sessionData.ExpiresAt = DateTime.UtcNow.AddMinutes(_appSettings.Redis.SessionExpiryMinutes);
                await _dbContext.SaveChangesAsync();

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session context for {SessionId}", sessionId);
                return new SessionContext { SessionId = sessionId };
            }
        }

        public async Task SaveContextAsync(SessionContext context)
        {
            try
            {
                var sessionData = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == context.SessionId);

                string json = JsonSerializer.Serialize(context);

                if (sessionData == null)
                {
                    // Create new session
                    sessionData = new SessionData
                    {
                        SessionId = context.SessionId,
                        Data = json,
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(_appSettings.Redis.SessionExpiryMinutes)
                    };

                    _dbContext.Sessions.Add(sessionData);
                }
                else
                {
                    // Update existing session
                    sessionData.Data = json;
                    sessionData.LastUpdatedAt = DateTime.UtcNow;
                    sessionData.ExpiresAt = DateTime.UtcNow.AddMinutes(_appSettings.Redis.SessionExpiryMinutes);

                    _dbContext.Sessions.Update(sessionData);
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogDebug("Saved context for {SessionId} with {MessageCount} messages",
                    context.SessionId, context.Messages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving session context for {SessionId}", context.SessionId);
                throw;
            }
        }

        public async Task UpdateContextAsync(string sessionId, string userInput, string assistantResponse)
        {
            var context = await GetSessionContextAsync(sessionId);

            // Add user message
            context.Messages.Add(new Message
            {
                Role = "user",
                Content = userInput,
                Timestamp = DateTime.UtcNow,
                TokenCount = _tokenManager.CountTokens(userInput)
            });

            // Add assistant message
            context.Messages.Add(new Message
            {
                Role = "assistant",
                Content = assistantResponse,
                Timestamp = DateTime.UtcNow,
                TokenCount = _tokenManager.CountTokens(assistantResponse)
            });

            context.LastUpdatedAt = DateTime.UtcNow;
            context.TotalTokens = _tokenManager.CountContextTokens(context);

            await SaveContextAsync(context);
        }

        public async Task<bool> DeleteSessionAsync(string sessionId)
        {
            try
            {
                var sessionData = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (sessionData == null)
                {
                    return false;
                }

                _dbContext.Sessions.Remove(sessionData);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted session {SessionId}", sessionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                return false;
            }
        }
    }
}




