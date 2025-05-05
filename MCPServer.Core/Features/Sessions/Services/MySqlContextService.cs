using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Features.Sessions.Services.Interfaces;
using MCPServer.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Sessions.Services
{
    public class MySqlContextService : IContextService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<MySqlContextService> _logger;

        public MySqlContextService(
            McpServerDbContext dbContext,
            ILogger<MySqlContextService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<SessionContext?> GetContextAsync(string sessionId)
        {
            try
            {
                // Find the session in the database
                var session = await _dbContext.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    _logger.LogInformation("Session {SessionId} not found", sessionId);
                    return null;
                }

                // Deserialize the context
                var context = new SessionContext
                {
                    SessionId = session.SessionId,
                    Title = session.Title,
                    Username = session.Username,
                    CreatedAt = session.CreatedAt,
                    UpdatedAt = session.UpdatedAt
                };

                // Deserialize messages if available
                if (!string.IsNullOrEmpty(session.MessagesJson))
                {
                    try
                    {
                        context.Messages = JsonSerializer.Deserialize<List<Message>>(session.MessagesJson) ?? new List<Message>();
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Error deserializing messages for session {SessionId}", sessionId);
                        context.Messages = new List<Message>();
                    }
                }
                else
                {
                    context.Messages = new List<Message>();
                }

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting context for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> SaveContextAsync(SessionContext context)
        {
            try
            {
                // Find the session in the database
                var session = await _dbContext.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == context.SessionId);

                if (session == null)
                {
                    // Create a new session
                    session = new Session
                    {
                        SessionId = context.SessionId,
                        Title = context.Title,
                        Username = context.Username,
                        CreatedAt = context.CreatedAt,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _dbContext.Sessions.Add(session);
                }
                else
                {
                    // Update the existing session
                    session.Title = context.Title;
                    session.Username = context.Username;
                    session.UpdatedAt = DateTime.UtcNow;
                }

                // Serialize the messages
                session.MessagesJson = JsonSerializer.Serialize(context.Messages);

                // Save changes
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving context for session {SessionId}", context.SessionId);
                throw;
            }
        }

        public async Task<List<string>> GetSessionIdsAsync(string? username = null)
        {
            try
            {
                // Query for sessions
                IQueryable<Session> query = _dbContext.Sessions;

                // Filter by username if provided
                if (!string.IsNullOrEmpty(username))
                {
                    query = query.Where(s => s.Username == username);
                }

                // Get the session IDs
                return await query
                    .OrderByDescending(s => s.UpdatedAt)
                    .Select(s => s.SessionId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session IDs for user {Username}", username);
                throw;
            }
        }

        public async Task<bool> DeleteContextAsync(string sessionId)
        {
            try
            {
                // Find the session in the database
                var session = await _dbContext.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    _logger.LogInformation("Session {SessionId} not found for deletion", sessionId);
                    return false;
                }

                // Remove the session
                _dbContext.Sessions.Remove(session);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting context for session {SessionId}", sessionId);
                throw;
            }
        }
    }
}
