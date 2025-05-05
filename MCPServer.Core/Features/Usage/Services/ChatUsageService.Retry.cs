using System;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;

namespace MCPServer.Core.Features.Usage.Services
{
    public partial class ChatUsageService
    {
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

                    // If the request was not successful or contains error text, ensure no costs are charged
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
                            _logger.LogInformation("Request already marked as failed in retry");
                        }
                        else
                        {
                            _logger.LogWarning("Detected error in response but success flag was true in retry. Overriding to false.");
                            success = false;
                        }

                        // Set both input and output tokens to 0 for failed requests
                        inputTokens = 0;
                        outputTokens = 0;
                        estimatedCost = 0;
                        _logger.LogInformation("Setting input and output tokens to 0 and cost to 0 in retry");
                    }

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
                        // For failed requests, set Value to 0 as well
                        Value = success ? (inputTokens + outputTokens) : 0,
                        SessionId = sessionId,
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            modelId = model?.Id,
                            modelName = model?.Name,
                            // For failed requests, ensure all token counts and costs are 0 in the JSON data too
                            inputTokens = success ? inputTokens : 0,
                            outputTokens = success ? outputTokens : 0,
                            estimatedCost = success ? estimatedCost : 0
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

                // If the request was not successful or contains error text, ensure no costs are charged
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
                        _logger.LogInformation("Request already marked as failed in ADO.NET fallback");
                    }
                    else
                    {
                        _logger.LogWarning("Detected error in response but success flag was true in ADO.NET fallback. Overriding to false.");
                        success = false;
                    }

                    // Set both input and output tokens to 0 for failed requests
                    inputTokens = 0;
                    outputTokens = 0;
                    estimatedCost = 0;
                    _logger.LogInformation("Setting input and output tokens to 0 and cost to 0 in ADO.NET fallback");
                }

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

                string additionalData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    modelId = model?.Id,
                    modelName = model?.Name,
                    providerId = model?.ProviderId,
                    providerName = model?.Provider?.Name,
                    // For failed requests, ensure all token counts and costs are 0 in the JSON data too
                    inputTokens = success ? inputTokens : 0,
                    outputTokens = success ? outputTokens : 0,
                    estimatedCost = success ? estimatedCost : 0
                });

                metricCmd.Parameters.AddWithValue("@metricType", "ChatTokensUsed");
                // For failed requests, set Value to 0 as well
                metricCmd.Parameters.AddWithValue("@value", success ? (inputTokens + outputTokens) : 0);
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
    }
}
