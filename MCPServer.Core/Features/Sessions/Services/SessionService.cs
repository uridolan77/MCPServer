using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using MySql.EntityFrameworkCore.Extensions;
using MCPServer.Core.Features.Sessions.Services.Interfaces;

namespace MCPServer.Core.Features.Sessions.Services
{
    public class SessionService : ISessionService
    {
        private readonly IDbContextFactory<McpServerDbContext> _dbContextFactory;
        private readonly ILogger<SessionService> _logger;
        private readonly AppSettings _appSettings;
        private readonly string _connectionString;

        public SessionService(
            IDbContextFactory<McpServerDbContext> dbContextFactory,
            ILogger<SessionService> logger,
            IOptions<AppSettings> appSettings,
            IConfiguration configuration)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _appSettings = appSettings.Value;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               "Server=localhost;Database=mcpserver_db;User=root;Password=password;";
        }

        public async Task<List<Message>> GetSessionHistoryAsync(string sessionId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var sessionData = await dbContext.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (sessionData == null || string.IsNullOrEmpty(sessionData.Data))
            {
                _logger.LogInformation("No session history found for session {SessionId}", sessionId);
                return new List<Message>();
            }

            try
            {
                // Deserialize the session data to get message history
                var sessionDataObject = JsonSerializer.Deserialize<SessionDataDto>(sessionData.Data);
                if (sessionDataObject?.Messages != null && sessionDataObject.Messages.Count > 0)
                {
                    _logger.LogInformation("Retrieved {Count} messages from session history", sessionDataObject.Messages.Count);
                    return sessionDataObject.Messages;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing session data for {SessionId}", sessionId);
            }

            return new List<Message>();
        }

        public async Task SaveSessionDataAsync(string sessionId, List<Message> sessionHistory)
        {
            try
            {
                _logger.LogInformation("Saving session data for session {SessionId}", sessionId);
                
                // Create a fresh DbContext for this operation to avoid the disposed service provider issue
                using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                    
                // Create SessionData DTO object with message history
                var dataContainer = new SessionDataDto
                {
                    Messages = sessionHistory,
                    Metadata = new Dictionary<string, string>
                    {
                        { "LastUpdated", DateTime.UtcNow.ToString("o") }
                    }
                };

                // Serialize the data container to JSON
                string serializedJson = JsonSerializer.Serialize(dataContainer);

                try
                {
                    // Try to find the existing session - use FirstOrDefaultAsync directly instead of AsNoTracking
                    // to avoid potential service provider issues
                    var existingSession = await dbContext.Sessions
                        .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                    if (existingSession == null)
                    {
                        // Create brand new SessionData entity for the database
                        var newSession = new Core.Models.SessionData
                        {
                            SessionId = sessionId,
                            Data = serializedJson,
                            CreatedAt = DateTime.UtcNow,
                            LastUpdatedAt = DateTime.UtcNow,
                            ExpiresAt = DateTime.UtcNow.AddMinutes(_appSettings.Redis?.SessionExpiryMinutes ?? 60) // Default to 1 hour expiry
                        };
                        
                        // Add the new entity to the database context
                        dbContext.Sessions.Add(newSession);
                    }
                    else
                    {
                        // Update the existing entity directly to avoid tracking issues
                        existingSession.Data = serializedJson;
                        existingSession.LastUpdatedAt = DateTime.UtcNow;
                        existingSession.ExpiresAt = DateTime.UtcNow.AddMinutes(_appSettings.Redis?.SessionExpiryMinutes ?? 60); // Reset expiry
                        
                        // Mark as modified
                        dbContext.Entry(existingSession).State = EntityState.Modified;
                    }

                    // Save changes to the database
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Session data saved successfully for session {SessionId}", sessionId);
                }
                catch (ObjectDisposedException odEx)
                {
                    _logger.LogWarning(odEx, "Detected disposed object during session save. Retrying with a fresh context for session {SessionId}", sessionId);
                    
                    // Try again with a completely isolated context - more robust than just using the factory
                    await RetrySessionSaveAsync(sessionId, serializedJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save session data for session {SessionId}", sessionId);
                // Don't rethrow in background operations to avoid crashing the process
            }
        }

        private async Task RetrySessionSaveAsync(string sessionId, string serializedJson)
        {
            try
            {
                _logger.LogInformation("Retrying session save with completely independent context for session {SessionId}", sessionId);

                // Get a completely fresh context with no shared service provider
                var options = new DbContextOptionsBuilder<McpServerDbContext>()
                    .UseApplicationServiceProvider(null) // Disconnect from any disposed service provider
                    .UseMySQL(_connectionString) // Use MySQL.EntityFrameworkCore provider
                    .EnableSensitiveDataLogging()
                    .Options;

                try
                {
                    // Create a completely new DbContext with these options
                    using var freshContext = new McpServerDbContext(options);
                    
                    // Get the expiry minutes value
                    int expiryMinutes = _appSettings.Redis?.SessionExpiryMinutes ?? 60;
                    DateTime expiryTime = DateTime.UtcNow.AddMinutes(expiryMinutes);
                    
                    // Check if session exists
                    bool exists = await freshContext.Sessions.AnyAsync(s => s.SessionId == sessionId);
                    
                    if (!exists)
                    {
                        // Create a new session
                        var newSession = new Core.Models.SessionData
                        {
                            SessionId = sessionId,
                            Data = serializedJson,
                            CreatedAt = DateTime.UtcNow,
                            LastUpdatedAt = DateTime.UtcNow,
                            ExpiresAt = expiryTime
                        };
                        freshContext.Sessions.Add(newSession);
                    }
                    else
                    {
                        // Direct update approach
                        await freshContext.Sessions
                            .Where(s => s.SessionId == sessionId)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(e => e.Data, serializedJson)
                                .SetProperty(e => e.LastUpdatedAt, DateTime.UtcNow)
                                .SetProperty(e => e.ExpiresAt, expiryTime)
                            );
                    }
                    
                    await freshContext.SaveChangesAsync();
                    _logger.LogInformation("Session data saved successfully with independent context for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in session retry with independent context, attempting direct ADO.NET for session {SessionId}", sessionId);
                    await RetryWithAdoNetAsync(sessionId, serializedJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed all retry attempts to save session data for session {SessionId}", sessionId);
            }
        }

        // Final fallback using direct ADO.NET
        private async Task RetryWithAdoNetAsync(string sessionId, string serializedJson)
        {
            MySql.Data.MySqlClient.MySqlConnection? connection = null;
            
            try
            {
                _logger.LogInformation("Attempting direct ADO.NET connection for session {SessionId}", sessionId);
                
                // Create a direct connection with no dependency on EF Core
                connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                // First check if the session exists
                string checkSql = "SELECT COUNT(1) FROM Sessions WHERE SessionId = @sessionId";
                using var checkCmd = new MySql.Data.MySqlClient.MySqlCommand(checkSql, connection);
                checkCmd.Parameters.AddWithValue("@sessionId", sessionId);
                
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                
                if (count == 0)
                {
                    // Insert new session
                    string insertSql = @"
                        INSERT INTO Sessions
                        (SessionId, Data, CreatedAt, LastUpdatedAt, ExpiresAt)
                        VALUES
                        (@sessionId, @data, @createdAt, @lastUpdatedAt, @expiresAt)";
                        
                    using var insertCmd = new MySql.Data.MySqlClient.MySqlCommand(insertSql, connection);
                    
                    DateTime now = DateTime.UtcNow;
                    int expiryMinutes = _appSettings.Redis?.SessionExpiryMinutes ?? 60;
                    
                    insertCmd.Parameters.AddWithValue("@sessionId", sessionId);
                    insertCmd.Parameters.AddWithValue("@data", serializedJson);
                    insertCmd.Parameters.AddWithValue("@createdAt", now);
                    insertCmd.Parameters.AddWithValue("@lastUpdatedAt", now);
                    insertCmd.Parameters.AddWithValue("@expiresAt", now.AddMinutes(expiryMinutes));
                    
                    int result = await insertCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Direct MySQL insert successful, rows affected: {Result}", result);
                }
                else
                {
                    // Update existing session
                    string updateSql = @"
                        UPDATE Sessions 
                        SET Data = @data, 
                            LastUpdatedAt = @lastUpdatedAt, 
                            ExpiresAt = @expiresAt
                        WHERE SessionId = @sessionId";
                        
                    using var updateCmd = new MySql.Data.MySqlClient.MySqlCommand(updateSql, connection);
                    
                    DateTime now = DateTime.UtcNow;
                    int expiryMinutes = _appSettings.Redis?.SessionExpiryMinutes ?? 60;
                    
                    updateCmd.Parameters.AddWithValue("@sessionId", sessionId);
                    updateCmd.Parameters.AddWithValue("@data", serializedJson);
                    updateCmd.Parameters.AddWithValue("@lastUpdatedAt", now);
                    updateCmd.Parameters.AddWithValue("@expiresAt", now.AddMinutes(expiryMinutes));
                    
                    int result = await updateCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Direct MySQL update successful, rows affected: {Result}", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during ADO.NET direct MySQL access for session {SessionId}", sessionId);
            }
            finally
            {
                if (connection != null)
                {
                    await connection.CloseAsync();
                    await connection.DisposeAsync();
                }
            }
        }

        public async Task<bool> DeleteSessionAsync(string sessionId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            try
            {
                var sessionData = await dbContext.Sessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (sessionData == null)
                {
                    return false;
                }

                dbContext.Sessions.Remove(sessionData);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted session {SessionId}", sessionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<List<SessionData>> GetAllSessionsAsync(int page, int pageSize)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            try
            {
                var sessions = await dbContext.Sessions
                    .AsNoTracking()
                    .OrderByDescending(s => s.LastUpdatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} sessions, page {Page}, pageSize {PageSize}", 
                    sessions.Count, page, pageSize);
                    
                return sessions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all sessions, page {Page}, pageSize {PageSize}", 
                    page, pageSize);
                return new List<SessionData>();
            }
        }

        public async Task<SessionData> GetSessionDataAsync(string sessionId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            try
            {
                var sessionData = await dbContext.Sessions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (sessionData == null)
                {
                    _logger.LogInformation("Session data not found for session {SessionId}", sessionId);
                    return null;
                }

                // Update last accessed time
                try
                {
                    var trackingSession = await dbContext.Sessions.FindAsync(sessionId);
                    if (trackingSession != null)
                    {
                        trackingSession.LastUpdatedAt = DateTime.UtcNow;
                        trackingSession.ExpiresAt = DateTime.UtcNow.AddMinutes(_appSettings.Redis?.SessionExpiryMinutes ?? 60);
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to update last accessed time for session {SessionId}", sessionId);
                    // We still want to return the session data even if we can't update the timestamp
                }

                _logger.LogInformation("Retrieved session data for session {SessionId}", sessionId);
                return sessionData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving session data for session {SessionId}", sessionId);
                return null;
            }
        }
    }
}



