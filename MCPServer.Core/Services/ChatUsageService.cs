using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Models.Responses;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySql.EntityFrameworkCore.Extensions; // Add this for MySQL.EntityFrameworkCore

namespace MCPServer.Core.Services
{
    public class ChatUsageService : IChatUsageService
    {
        private readonly IDbContextFactory<McpServerDbContext> _dbContextFactory;
        private readonly ILogger<ChatUsageService> _logger;
        private readonly ITokenManager? _tokenManager;
        private readonly string _connectionString;

        public ChatUsageService(
            IDbContextFactory<McpServerDbContext> dbContextFactory,
            ILogger<ChatUsageService> logger,
            IConfiguration configuration,
            ITokenManager? tokenManager = null)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _tokenManager = tokenManager;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               "Server=localhost;Database=mcpserver_db;User=root;Password=password;";
        }

        public async Task LogChatUsageAsync(
            string sessionId, 
            string message, 
            string response, 
            LlmModel? model, 
            int duration, 
            bool success, 
            string? errorMessage = null, 
            List<Message>? sessionHistory = null,
            string? username = null)
        {
            try
            {
                _logger.LogInformation("Logging chat usage for session {SessionId}", sessionId);
                
                // Create a fresh DbContext for this operation to avoid disposed service provider issues
                using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                    
                // Get token counts
                int inputTokens = 0;
                int outputTokens = 0;
                decimal estimatedCost = 0;

                // Use an ITokenManager instance to count tokens if available
                if (_tokenManager != null)
                {
                    _logger.LogInformation("Using TokenManager for counting tokens");
                    inputTokens = _tokenManager.CountTokens(message);
                    outputTokens = _tokenManager.CountTokens(response);

                    // Add history tokens to input if available
                    if (sessionHistory != null)
                    {
                        foreach (var historyMessage in sessionHistory)
                        {
                            inputTokens += _tokenManager.CountTokens(historyMessage.Content);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("TokenManager not available, using fallback token counting");
                    // Fallback estimation: ~4 chars per token as rough estimate
                    inputTokens = message.Length / 4;
                    outputTokens = response.Length / 4;

                    // Add history tokens to input if available
                    if (sessionHistory != null)
                    {
                        inputTokens += sessionHistory.Sum(m => m.Content.Length) / 4;
                    }
                }

                _logger.LogInformation("Counted tokens: {InputTokens} input, {OutputTokens} output", inputTokens, outputTokens);

                try
                {
                    // Calculate estimated cost if model information is available
                    if (model != null)
                    {
                        // Most models charge per 1K tokens
                        decimal inputCostPer1K = model.CostPer1KInputTokens;
                        decimal outputCostPer1K = model.CostPer1KOutputTokens;

                        estimatedCost = (inputTokens / 1000.0M) * inputCostPer1K +
                                       (outputTokens / 1000.0M) * outputCostPer1K;
                        
                        _logger.LogInformation("Calculated cost: ${EstimatedCost:F6} using model {ModelName}", estimatedCost, model?.Name);
                    }

                    // Ensure we have the provider information if a model is specified
                    if (model != null && model.Provider == null && model.ProviderId > 0)
                    {
                        // Try to load the provider
                        var provider = await dbContext.LlmProviders.FindAsync(model.ProviderId);
                        if (provider != null)
                        {
                            model.Provider = provider;
                            _logger.LogInformation("Loaded provider: {ProviderName}", provider.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Could not find provider with ID {ProviderId}", model.ProviderId);
                        }
                    }

                    // Create the usage log entry
                    var usageLog = new ChatUsageLog
                    {
                        SessionId = sessionId,
                        ModelId = model?.Id,
                        ModelName = model?.Name,
                        ProviderId = model?.ProviderId,
                        ProviderName = model?.Provider?.Name,
                        Message = message,
                        Response = response,
                        InputTokenCount = inputTokens,
                        OutputTokenCount = outputTokens,
                        EstimatedCost = estimatedCost,
                        Duration = duration,
                        Success = success,
                        ErrorMessage = errorMessage,
                        Timestamp = DateTime.UtcNow
                    };

                    // Try to get user from username if provided
                    if (!string.IsNullOrEmpty(username))
                    {
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                        if (user != null)
                        {
                            usageLog.UserId = user.Id;
                            _logger.LogInformation("Added userId {UserId} for user {Username} to usage log", user.Id, username);
                        }
                    }

                    _logger.LogInformation("Saving ChatUsageLog to database for session {SessionId}", sessionId);
                    
                    // Save the usage log to the database
                    dbContext.ChatUsageLogs.Add(usageLog);
                    var saveResult = await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("ChatUsageLog save result: {SaveResult} records affected", saveResult);

                    // Also log as a usage metric
                    var usageMetric = new UsageMetric
                    {
                        MetricType = "ChatTokensUsed",
                        Value = inputTokens + outputTokens,
                        SessionId = sessionId,
                        UserId = usageLog.UserId,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            modelId = model?.Id,
                            modelName = model?.Name,
                            providerId = model?.ProviderId,
                            providerName = model?.Provider?.Name,
                            inputTokens,
                            outputTokens,
                            estimatedCost
                        })
                    };

                    dbContext.UsageMetrics.Add(usageMetric);
                    var metricSaveResult = await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("UsageMetric save result: {SaveResult} records affected", metricSaveResult);
                    _logger.LogInformation("Chat usage logged successfully for session {SessionId}", sessionId);
                }
                catch (ObjectDisposedException odEx)
                {
                    _logger.LogWarning(odEx, "Detected disposed object during usage logging. Retrying with a fresh context for session {SessionId}", sessionId);
                    
                    // Try again with a completely fresh context
                    await RetryLogChatUsageAsync(
                        sessionId,
                        message,
                        response,
                        model,
                        duration,
                        success,
                        errorMessage,
                        inputTokens,
                        outputTokens,
                        estimatedCost,
                        username
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log chat usage for session {SessionId}", sessionId);
                // Don't rethrow as logging failures shouldn't break the chat flow
            }
        }

        private async Task RetryLogChatUsageAsync(
            string sessionId,
            string message,
            string response,
            LlmModel? model,
            int duration,
            bool success,
            string? errorMessage,
            int inputTokens,
            int outputTokens,
            decimal estimatedCost,
            string? username)
        {
            try
            {
                _logger.LogInformation("Retrying chat usage log with completely new context for session {SessionId}", sessionId);

                // Get a completely fresh context with a minimal service provider for the retry
                var options = new DbContextOptionsBuilder<McpServerDbContext>()
                    .UseApplicationServiceProvider(null) // Disconnect from any disposed service provider
                    .UseMySQL(_connectionString) // Use MySQL.EntityFrameworkCore provider
                    .EnableSensitiveDataLogging()
                    .Options;

                try
                {
                    // Create a completely new DbContext with these options
                    using var freshContext = new McpServerDbContext(options);
                    
                    // Use a simpler approach - create entities directly without loading anything else
                    var usageLog = new ChatUsageLog
                    {
                        SessionId = sessionId,
                        ModelId = model?.Id,
                        ModelName = model?.Name,
                        ProviderId = model?.ProviderId,
                        ProviderName = model?.Provider?.Name,
                        Message = message,
                        Response = response,
                        InputTokenCount = inputTokens,
                        OutputTokenCount = outputTokens,
                        EstimatedCost = estimatedCost,
                        Duration = duration,
                        Success = success,
                        ErrorMessage = errorMessage,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    // Skip trying to load user
                    freshContext.ChatUsageLogs.Add(usageLog);
                    await freshContext.SaveChangesAsync();
                    
                    _logger.LogInformation("ChatUsageLog saved successfully on retry with fresh context");
                    
                    // Also add a simple usage metric
                    var usageMetric = new UsageMetric
                    {
                        MetricType = "ChatTokensUsed",
                        Value = inputTokens + outputTokens,
                        SessionId = sessionId,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            modelId = model?.Id,
                            modelName = model?.Name,
                            inputTokens,
                            outputTokens,
                            estimatedCost
                        })
                    };
                    
                    freshContext.UsageMetrics.Add(usageMetric);
                    await freshContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Usage metric saved successfully on retry");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in retry with fresh context, attempting pure ADO.NET");
                    await RetryWithAdoNetAsync(sessionId, message, response, model, duration, success, errorMessage,
                        inputTokens, outputTokens, estimatedCost);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log chat usage on retry for session {SessionId}", sessionId);
            }
        }

        // Final fallback using direct ADO.NET instead of Entity Framework
        private async Task RetryWithAdoNetAsync(
            string sessionId,
            string message,
            string response,
            LlmModel? model,
            int duration,
            bool success,
            string? errorMessage,
            int inputTokens,
            int outputTokens,
            decimal estimatedCost)
        {
            MySql.Data.MySqlClient.MySqlConnection? connection = null;
            
            try
            {
                _logger.LogInformation("Attempting direct ADO.NET connection with MySQL for session {SessionId}", sessionId);
                
                // Create a direct ADO.NET connection with no dependency on EF Core
                connection = new MySql.Data.MySqlClient.MySqlConnection(_connectionString);
                await connection.OpenAsync();
                
                string sql = @"
                    INSERT INTO ChatUsageLogs 
                    (SessionId, ModelId, ModelName, ProviderId, ProviderName, Message, Response, 
                    InputTokenCount, OutputTokenCount, EstimatedCost, Duration, Success, 
                    ErrorMessage, Timestamp) 
                    VALUES 
                    (@sessionId, @modelId, @modelName, @providerId, @providerName, @message, @response, 
                    @inputTokens, @outputTokens, @estimatedCost, @duration, @success, 
                    @errorMessage, @timestamp);";
                
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, connection);
                
                // Add parameters - MySQL uses different parameter handling
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                cmd.Parameters.AddWithValue("@modelId", model?.Id ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@modelName", model?.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@providerId", model?.ProviderId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@providerName", model?.Provider?.Name ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.Parameters.AddWithValue("@response", response);
                cmd.Parameters.AddWithValue("@inputTokens", inputTokens);
                cmd.Parameters.AddWithValue("@outputTokens", outputTokens);
                cmd.Parameters.AddWithValue("@estimatedCost", estimatedCost);
                cmd.Parameters.AddWithValue("@duration", duration);
                cmd.Parameters.AddWithValue("@success", success);
                cmd.Parameters.AddWithValue("@errorMessage", errorMessage ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                
                int result = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Direct MySQL insert successful, rows affected: {Result}", result);
                
                // Also add the usage metric
                string metricSql = @"
                    INSERT INTO UsageMetrics 
                    (MetricType, Value, SessionId, Timestamp, AdditionalData) 
                    VALUES 
                    (@metricType, @value, @sessionId, @timestamp, @additionalData);";
                    
                using var metricCmd = new MySql.Data.MySqlClient.MySqlCommand(metricSql, connection);
                
                string additionalData = JsonSerializer.Serialize(new
                {
                    modelId = model?.Id,
                    modelName = model?.Name,
                    providerId = model?.ProviderId,
                    providerName = model?.Provider?.Name,
                    inputTokens,
                    outputTokens,
                    estimatedCost
                });
                
                metricCmd.Parameters.AddWithValue("@metricType", "ChatTokensUsed");
                metricCmd.Parameters.AddWithValue("@value", inputTokens + outputTokens);
                metricCmd.Parameters.AddWithValue("@sessionId", sessionId);
                metricCmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                metricCmd.Parameters.AddWithValue("@additionalData", additionalData);
                
                int metricResult = await metricCmd.ExecuteNonQueryAsync();
                _logger.LogInformation("Direct MySQL metric insert successful, rows affected: {Result}", metricResult);
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

        // Implement additional methods required by the interface
        public async Task<ChatUsageStatsResponse> GetOverallStatsAsync()
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var usageLogs = await dbContext.ChatUsageLogs.ToListAsync();

            // Calculate overall stats
            var response = new ChatUsageStatsResponse
            {
                TotalMessages = usageLogs.Count,
                TotalTokensUsed = usageLogs.Sum(l => l.InputTokenCount + l.OutputTokenCount),
                TotalCost = usageLogs.Sum(l => l.EstimatedCost),
                ModelStats = GetModelStatsFromChatLogs(usageLogs),
                ProviderStats = GetProviderStatsFromChatLogs(usageLogs)
            };

            return response;
        }

        public async Task<List<ChatUsageLogResponse>> GetFilteredLogsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int? modelId,
            int? providerId,
            string? sessionId,
            int page = 1,
            int pageSize = 20)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            // Start with all logs
            IQueryable<ChatUsageLog> query = dbContext.ChatUsageLogs;

            // Apply filters
            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            if (modelId.HasValue)
                query = query.Where(l => l.ModelId == modelId.Value);

            if (providerId.HasValue)
                query = query.Where(l => l.ProviderId == providerId.Value);

            if (!string.IsNullOrEmpty(sessionId))
                query = query.Where(l => l.SessionId == sessionId);

            // Order by timestamp descending and apply pagination
            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Map to response objects
            var response = logs.Select(l => new ChatUsageLogResponse
            {
                Id = l.Id,
                SessionId = l.SessionId,
                UserId = l.UserId,
                ModelId = l.ModelId ?? 0,
                ModelName = l.ModelName ?? "Unknown",
                ProviderId = l.ProviderId ?? 0,
                ProviderName = l.ProviderName ?? "Unknown",
                Message = l.Message, // Include message content
                Response = l.Response, // Include response content
                InputTokens = l.InputTokenCount,
                OutputTokens = l.OutputTokenCount,
                EstimatedCost = l.EstimatedCost,
                DurationMs = l.Duration,
                IsSuccessful = l.Success,
                ErrorMessage = l.ErrorMessage,
                Timestamp = l.Timestamp
            }).ToList();

            return response;
        }

        public async Task<ModelUsageStatResponse?> GetModelStatsAsync(int modelId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            // Get model name from database if possible
            string modelName = "Unknown";
            var model = await dbContext.LlmModels.FindAsync(modelId);
            if (model != null)
            {
                modelName = model.Name;
            }

            // Get all chat usage logs for this model
            var chatLogs = await dbContext.ChatUsageLogs
                .Where(l => l.ModelId == modelId)
                .ToListAsync();

            // If no chat logs found but we have the model, check if it's a valid model
            if (chatLogs.Count == 0 && model == null)
            {
                return null;
            }

            // Compile the statistics from the chat logs
            var response = new ModelUsageStatResponse
            {
                ModelId = modelId,
                ModelName = chatLogs.FirstOrDefault()?.ModelName ?? modelName,
                MessagesCount = chatLogs.Count,
                InputTokens = chatLogs.Sum(l => l.InputTokenCount),
                OutputTokens = chatLogs.Sum(l => l.OutputTokenCount),
                TotalTokens = chatLogs.Sum(l => l.InputTokenCount + l.OutputTokenCount),
                EstimatedCost = chatLogs.Sum(l => l.EstimatedCost)
            };

            return response;
        }

        public async Task<ProviderUsageStatResponse?> GetProviderStatsAsync(int providerId)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            // Get provider name from database if possible
            string providerName = "Unknown";
            var provider = await dbContext.LlmProviders.FindAsync(providerId);
            if (provider != null)
            {
                providerName = provider.Name;
            }

            // Get all chat usage logs for this provider
            var chatLogs = await dbContext.ChatUsageLogs
                .Where(l => l.ProviderId == providerId)
                .ToListAsync();

            // If no chat logs found but we have the provider, check if it's a valid provider
            if (chatLogs.Count == 0 && provider == null)
            {
                return null;
            }

            // Compile the statistics from the chat logs
            var response = new ProviderUsageStatResponse
            {
                ProviderId = providerId,
                ProviderName = chatLogs.FirstOrDefault()?.ProviderName ?? providerName,
                MessagesCount = chatLogs.Count,
                TotalTokens = chatLogs.Sum(l => l.InputTokenCount + l.OutputTokenCount),
                EstimatedCost = chatLogs.Sum(l => l.EstimatedCost)
            };

            return response;
        }
        
        // Helper methods for generating statistics
        private List<ModelUsageStatResponse> GetModelStatsFromChatLogs(List<ChatUsageLog> logs)
        {
            var modelStats = logs
                .Where(l => l.ModelId.HasValue)
                .GroupBy(l => l.ModelId)
                .Select(g => new ModelUsageStatResponse
                {
                    ModelId = g.Key ?? 0,
                    ModelName = g.First().ModelName ?? "Unknown",
                    MessagesCount = g.Count(),
                    InputTokens = g.Sum(l => l.InputTokenCount),
                    OutputTokens = g.Sum(l => l.OutputTokenCount),
                    TotalTokens = g.Sum(l => l.InputTokenCount + l.OutputTokenCount),
                    EstimatedCost = g.Sum(l => l.EstimatedCost)
                })
                .ToList();

            return modelStats;
        }

        private List<ProviderUsageStatResponse> GetProviderStatsFromChatLogs(List<ChatUsageLog> logs)
        {
            var providerStats = logs
                .Where(l => l.ProviderId.HasValue)
                .GroupBy(l => l.ProviderId)
                .Select(g => new ProviderUsageStatResponse
                {
                    ProviderId = g.Key ?? 0,
                    ProviderName = g.First().ProviderName ?? "Unknown",
                    MessagesCount = g.Count(),
                    TotalTokens = g.Sum(l => l.InputTokenCount + l.OutputTokenCount),
                    EstimatedCost = g.Sum(l => l.EstimatedCost)
                })
                .ToList();

            return providerStats;
        }

#if DEBUG
        public async Task<object> GetDiagnosticInfoAsync()
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            // Check how many logs are in the database
            var totalCount = await dbContext.ChatUsageLogs.CountAsync();
            
            // Get a few sample logs if any exist
            var sampleLogs = await dbContext.ChatUsageLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(5)
                .ToListAsync();
            
            // Get counts from UsageMetrics as well for comparison
            var metricsCount = await dbContext.UsageMetrics
                .Where(m => m.MetricType == "ChatTokensUsed")
                .CountAsync();
            
            // Return diagnostic information
            return new
            {
                chatUsageLogsCount = totalCount,
                chatMetricsCount = metricsCount,
                sampleLogs = sampleLogs.Select(l => new
                {
                    id = l.Id,
                    sessionId = l.SessionId,
                    modelName = l.ModelName,
                    providerName = l.ProviderName,
                    messagePreview = l.Message?.Length > 50 ? l.Message.Substring(0, 50) + "..." : l.Message,
                    inputTokens = l.InputTokenCount,
                    outputTokens = l.OutputTokenCount,
                    timestamp = l.Timestamp
                }).ToList(),
                tablesInContext = string.Join(", ", dbContext.GetType().GetProperties()
                    .Where(p => p.PropertyType.IsGenericType && 
                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .Select(p => p.Name))
            };
        }

        public async Task<object> GenerateSampleDataAsync(int count = 10)
        {
            using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            
            Random random = new Random();
            DateTime startDate = DateTime.Now.AddDays(-30); // Last 30 days
            List<ChatUsageLog> sampleLogs = new List<ChatUsageLog>();
            
            // Get available models and providers
            var models = await dbContext.LlmModels.Include(m => m.Provider).ToListAsync();
            
            if (models.Count == 0)
            {
                // Create some sample models and providers if none exist
                var openAiProvider = new LlmProvider
                {
                    Name = "OpenAI",
                    DisplayName = "OpenAI",
                    ApiEndpoint = "https://api.openai.com/v1",
                    Description = "OpenAI API provider",
                    IsEnabled = true,
                    AuthType = "ApiKey",
                    ConfigSchema = "{}",
                    CreatedAt = DateTime.UtcNow
                };
                
                var anthropicProvider = new LlmProvider
                {
                    Name = "Anthropic",
                    DisplayName = "Anthropic",
                    ApiEndpoint = "https://api.anthropic.com",
                    Description = "Anthropic API provider",
                    IsEnabled = true,
                    AuthType = "ApiKey",
                    ConfigSchema = "{}",
                    CreatedAt = DateTime.UtcNow
                };
                
                dbContext.LlmProviders.Add(openAiProvider);
                dbContext.LlmProviders.Add(anthropicProvider);
                await dbContext.SaveChangesAsync();
                
                var gpt4Model = new LlmModel
                {
                    Name = "gpt-4",
                    Description = "GPT-4 by OpenAI",
                    MaxTokens = 8192,
                    Provider = openAiProvider,
                    ProviderId = openAiProvider.Id,
                    CostPer1KInputTokens = 0.03M,
                    CostPer1KOutputTokens = 0.06M,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                var claudeModel = new LlmModel
                {
                    Name = "claude-3-opus",
                    Description = "Claude 3 Opus by Anthropic",
                    MaxTokens = 100000,
                    Provider = anthropicProvider,
                    ProviderId = anthropicProvider.Id,
                    CostPer1KInputTokens = 0.015M,
                    CostPer1KOutputTokens = 0.075M,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                dbContext.LlmModels.Add(gpt4Model);
                dbContext.LlmModels.Add(claudeModel);
                await dbContext.SaveChangesAsync();
                
                models = await dbContext.LlmModels.Include(m => m.Provider).ToListAsync();
            }
            
            // Generate sample logs
            for (int i = 0; i < count; i++)
            {
                var model = models[random.Next(models.Count)];
                var provider = model.Provider;
                
                bool isSuccessful = random.NextDouble() > 0.1; // 90% success rate
                
                int inputTokens = random.Next(100, 2000);
                int outputTokens = isSuccessful ? random.Next(50, 1500) : 0;
                
                // Calculate estimated cost
                decimal inputCost = (inputTokens / 1000.0M) * model.CostPer1KInputTokens;
                decimal outputCost = (outputTokens / 1000.0M) * model.CostPer1KOutputTokens;
                decimal totalCost = inputCost + outputCost;
                
                var log = new ChatUsageLog
                {
                    SessionId = Guid.NewGuid().ToString(),
                    UserId = Guid.NewGuid(),
                    ModelId = model.Id,
                    ModelName = model.Name,
                    ProviderId = provider.Id,
                    ProviderName = provider.Name,
                    Message = GetRandomQuestion(),
                    Response = isSuccessful ? GetRandomResponse() : "",
                    InputTokenCount = inputTokens,
                    OutputTokenCount = outputTokens,
                    EstimatedCost = totalCost,
                    Duration = random.Next(500, 5000), // between 0.5 and 5 seconds
                    Success = isSuccessful,
                    ErrorMessage = isSuccessful ? null : GetRandomError(),
                    Timestamp = startDate.AddDays(random.Next(30)).AddHours(random.Next(24)).AddMinutes(random.Next(60))
                };
                
                sampleLogs.Add(log);
            }
            
            // Add the logs to the database
            dbContext.ChatUsageLogs.AddRange(sampleLogs);
            await dbContext.SaveChangesAsync();
            
            return new
            {
                generatedCount = sampleLogs.Count,
                totalLogsCount = await dbContext.ChatUsageLogs.CountAsync(),
                sampleLogs = sampleLogs.Take(5).Select(l => new
                {
                    id = l.Id,
                    modelName = l.ModelName,
                    providerName = l.ProviderName,
                    inputTokens = l.InputTokenCount,
                    outputTokens = l.OutputTokenCount,
                    estimatedCost = l.EstimatedCost,
                    timestamp = l.Timestamp
                }).ToList()
            };
        }
        
        private string GetRandomQuestion()
        {
            string[] questions = {
                "Can you explain how quantum computing works?",
                "What are the best practices for secure API authentication?",
                "How do I implement a neural network from scratch?",
                "What's the difference between Docker and Kubernetes?",
                "Can you help me optimize this database query?",
                "What's the best way to learn a new programming language?",
                "How do I implement JWT authentication in my web app?",
                "Can you explain the SOLID principles of object-oriented design?",
                "What are microservices and when should they be used?",
                "How do I set up CI/CD for my project?"
            };
            
            return questions[new Random().Next(questions.Length)];
        }
        
        private string GetRandomResponse()
        {
            string[] responses = {
                "Quantum computing leverages quantum mechanics principles, such as superposition and entanglement, to perform computations. Unlike classical computers that use bits (0 or 1), quantum computers use quantum bits or qubits that can exist in multiple states simultaneously, allowing them to solve certain complex problems more efficiently.",
                "For secure API authentication, consider implementing OAuth 2.0 or JWT, use HTTPS exclusively, implement rate limiting, rotate API keys regularly, validate all inputs, and use strong authentication methods. Multi-factor authentication adds an extra layer of security for sensitive operations.",
                "To implement a neural network from scratch: 1) Define the network architecture (input, hidden, output layers) 2) Initialize weights randomly 3) Implement forward propagation 4) Define loss function 5) Implement backpropagation 6) Train the network using gradient descent 7) Test and evaluate performance.",
                "Docker is a container platform for packaging applications and dependencies. Kubernetes is an orchestration system for managing containerized applications at scale. Docker focuses on the container itself, while Kubernetes manages clusters of containers with features like load balancing, auto-scaling, and automated rollouts/rollbacks.",
                "To optimize a database query: 1) Add proper indexes 2) Avoid SELECT * 3) Use specific JOIN types 4) Limit result sets 5) Use EXPLAIN to analyze execution plans 6) Optimize WHERE clauses 7) Consider denormalizing for read-heavy workloads 8) Cache frequent queries when possible.",
                "To learn a new programming language effectively: 1) Start with fundamentals and syntax 2) Build small projects 3) Read documentation and books 4) Join communities for support 5) Contribute to open source 6) Teach others what you've learned 7) Practice regularly with coding challenges.",
                "To implement JWT authentication: 1) Install required packages 2) Create a secret key 3) Implement sign-in endpoint that validates credentials and returns a token 4) Create middleware to verify tokens 5) Secure routes with the middleware 6) Implement token refresh functionality 7) Handle token expiration and revocation.",
                "The SOLID principles are: Single Responsibility (a class should have one reason to change), Open-Closed (entities should be open for extension but closed for modification), Liskov Substitution (derived classes must be substitutable for their base classes), Interface Segregation (clients shouldn't depend on interfaces they don't use), and Dependency Inversion (depend on abstractions, not concretions).",
                "Microservices are an architectural style where applications are composed of small, independent services that communicate over a network. They're ideal when you need independent scaling, technology diversity, or organizational alignment with business capabilities. However, they add complexity in testing, deployment, and monitoring compared to monolithic applications.",
                "To set up CI/CD: 1) Choose a CI/CD platform like GitHub Actions, Jenkins, or CircleCI 2) Create configuration files defining your pipeline 3) Configure automated testing 4) Set up build processes 5) Implement deployment strategies 6) Add monitoring and notifications 7) Automate rollbacks for failed deployments."
            };
            
            return responses[new Random().Next(responses.Length)];
        }
        
        private string GetRandomError()
        {
            string[] errors = {
                "Rate limit exceeded. Please retry after 60 seconds.",
                "Authentication failed. Invalid API key.",
                "Model is currently overloaded. Please try again later.",
                "Request timed out after 30 seconds.",
                "Internal server error occurred while processing request.",
                "Invalid request format. Please check your parameters.",
                "Content policy violation detected in prompt."
            };
            
            return errors[new Random().Next(errors.Length)];
        }
#endif
    }
}