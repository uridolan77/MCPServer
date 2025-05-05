using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Models.Responses;
using MCPServer.Core.Features.Auth.Services.Interfaces;
using MCPServer.Core.Features.Usage.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MySql.EntityFrameworkCore.Extensions;

namespace MCPServer.Core.Features.Usage.Services
{
    public partial class ChatUsageService : IChatUsageService
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

                // Check if the response contains error messages or if success is explicitly false
                // This ensures we don't charge for any failed requests, regardless of how they're reported
                bool isErrorResponse = !success ||
                                      response.StartsWith("[ERROR_NO_BILLING]") ||
                                      response.Contains("Error connecting to OpenAI") ||
                                      response.Contains("Incorrect API key") ||
                                      response.Contains("Error") ||
                                      response.Contains("error") ||
                                      response.Contains("failed") ||
                                      response.Contains("Failed") ||
                                      response.Contains("Invalid") ||
                                      response.Contains("invalid") ||
                                      (!string.IsNullOrEmpty(errorMessage));

                // Always override success flag if we detect an error response
                if (isErrorResponse)
                {
                    if (!success)
                    {
                        _logger.LogInformation("Request already marked as failed, not counting tokens");
                    }
                    else
                    {
                        // If we detected an error but success flag wasn't set properly, override it
                        _logger.LogWarning("Detected error in response but success flag was true. Overriding to false.");
                        success = false;
                    }
                }

                // Use an ITokenManager instance to count tokens if available
                if (_tokenManager != null)
                {
                    _logger.LogInformation("Using TokenManager for counting tokens");

                    // Only count tokens if this is not an error response
                    if (!isErrorResponse)
                    {
                        inputTokens = _tokenManager.CountTokens(message);
                        outputTokens = _tokenManager.CountTokens(response);
                    }
                    else
                    {
                        _logger.LogInformation("Error response detected, not counting any tokens");
                        inputTokens = 0;
                        outputTokens = 0;
                    }

                    // If response has the error marker, remove it for display purposes
                    if (response.StartsWith("[ERROR_NO_BILLING]"))
                    {
                        response = response.Substring("[ERROR_NO_BILLING]".Length);
                    }

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

                    // Only count tokens if this is not an error response
                    if (!isErrorResponse)
                    {
                        // Fallback estimation: ~4 chars per token as rough estimate
                        inputTokens = message.Length / 4;
                        outputTokens = response.Length / 4;

                        // Add history tokens to input if available
                        if (sessionHistory != null)
                        {
                            inputTokens += sessionHistory.Sum(m => m.Content.Length) / 4;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Error response detected, not counting any tokens in fallback method");
                        inputTokens = 0;
                        outputTokens = 0;
                    }

                    // If response has the error marker, remove it for display purposes
                    if (response.StartsWith("[ERROR_NO_BILLING]"))
                    {
                        response = response.Substring("[ERROR_NO_BILLING]".Length);
                    }
                }

                _logger.LogInformation("Counted tokens: {InputTokens} input, {OutputTokens} output", inputTokens, outputTokens);

                try
                {
                    // Check again if this is an error response
                    if (!success || isErrorResponse)
                    {
                        _logger.LogInformation("Request was not successful or contains error, not charging for tokens");
                        // Set both input and output tokens to 0 for failed requests
                        // This ensures no costs are calculated for failed requests
                        inputTokens = 0;
                        outputTokens = 0;
                        estimatedCost = 0;

                        // Make sure success flag is set to false
                        success = false;
                    }
                    // Calculate estimated cost if model information is available and request was successful
                    else if (model != null)
                    {
                        // Only calculate costs for successful requests
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
                        // For failed requests, set Value to 0 as well
                        Value = success ? (inputTokens + outputTokens) : 0,
                        SessionId = sessionId,
                        UserId = usageLog.UserId,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            modelId = model?.Id,
                            modelName = model?.Name,
                            providerId = model?.ProviderId,
                            providerName = model?.Provider?.Name,
                            // For failed requests, ensure all token counts and costs are 0 in the JSON data too
                            inputTokens = success ? inputTokens : 0,
                            outputTokens = success ? outputTokens : 0,
                            estimatedCost = success ? estimatedCost : 0
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
    }
}
