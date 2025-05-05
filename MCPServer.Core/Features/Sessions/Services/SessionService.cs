using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Features.Sessions.Services.Interfaces;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using MCPServer.Core.Models;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly IContextService _contextService;
        private readonly ICachingService _cachingService;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            IContextService contextService,
            ICachingService cachingService,
            ILogger<SessionService> logger)
        {
            _contextService = contextService;
            _cachingService = cachingService;
            _logger = logger;
        }

        public async Task<SessionContext> GetSessionContextAsync(string sessionId)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"SessionContext_{sessionId}";
                if (_cachingService.TryGetValue(cacheKey, out SessionContext cachedContext))
                {
                    return cachedContext;
                }

                // If not in cache, get from database
                var context = await _contextService.GetContextAsync(sessionId);
                if (context == null)
                {
                    // Create a new context if none exists
                    context = new SessionContext
                    {
                        SessionId = sessionId,
                        Messages = new List<Message>(),
                        CreatedAt = DateTime.UtcNow
                    };

                    // Save the new context
                    await _contextService.SaveContextAsync(context);
                }

                // Cache the result
                _cachingService.Set(cacheKey, context, TimeSpan.FromMinutes(30));

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session context for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<SessionContext> AddMessageToSessionAsync(string sessionId, Message message)
        {
            try
            {
                // Get the current context
                var context = await GetSessionContextAsync(sessionId);

                // Add the message
                context.Messages.Add(message);
                context.UpdatedAt = DateTime.UtcNow;

                // Save the updated context
                await _contextService.SaveContextAsync(context);

                // Update the cache
                string cacheKey = $"SessionContext_{sessionId}";
                _cachingService.Set(cacheKey, context, TimeSpan.FromMinutes(30));

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding message to session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<SessionContext> ClearSessionAsync(string sessionId)
        {
            try
            {
                // Create a new empty context
                var context = new SessionContext
                {
                    SessionId = sessionId,
                    Messages = new List<Message>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Save the new context
                await _contextService.SaveContextAsync(context);

                // Update the cache
                string cacheKey = $"SessionContext_{sessionId}";
                _cachingService.Set(cacheKey, context, TimeSpan.FromMinutes(30));

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<List<string>> GetSessionIdsAsync(string? username = null)
        {
            try
            {
                // Get all session IDs from the context service
                return await _contextService.GetSessionIdsAsync(username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session IDs for user {Username}", username);
                throw;
            }
        }

        public async Task<bool> DeleteSessionAsync(string sessionId)
        {
            try
            {
                // Delete the session from the context service
                var result = await _contextService.DeleteContextAsync(sessionId);

                // Remove from cache if successful
                if (result)
                {
                    string cacheKey = $"SessionContext_{sessionId}";
                    _cachingService.Remove(cacheKey);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<SessionContext> AddUserToSessionAsync(string sessionId, string username)
        {
            try
            {
                // Get the current context
                var context = await GetSessionContextAsync(sessionId);

                // Set the username
                context.Username = username;
                context.UpdatedAt = DateTime.UtcNow;

                // Save the updated context
                await _contextService.SaveContextAsync(context);

                // Update the cache
                string cacheKey = $"SessionContext_{sessionId}";
                _cachingService.Set(cacheKey, context, TimeSpan.FromMinutes(30));

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {Username} to session {SessionId}", username, sessionId);
                throw;
            }
        }

        public async Task<SessionContext> SetSessionTitleAsync(string sessionId, string title)
        {
            try
            {
                // Get the current context
                var context = await GetSessionContextAsync(sessionId);

                // Set the title
                context.Title = title;
                context.UpdatedAt = DateTime.UtcNow;

                // Save the updated context
                await _contextService.SaveContextAsync(context);

                // Update the cache
                string cacheKey = $"SessionContext_{sessionId}";
                _cachingService.Set(cacheKey, context, TimeSpan.FromMinutes(30));

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting title for session {SessionId}", sessionId);
                throw;
            }
        }
    }
}
