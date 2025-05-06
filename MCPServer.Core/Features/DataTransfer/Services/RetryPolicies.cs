using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Data.SqlClient;

namespace MCPServer.Core.Features.DataTransfer.Services
{
    public static class RetryPolicies
    {
        public static AsyncRetryPolicy CreateSqlRetryPolicy(ILogger logger, int maxRetries = 3)
        {
            return Policy
                .Handle<SqlException>(ex => ex.Number != 1205) // Retry all except deadlock errors
                .Or<TimeoutException>()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(
                    maxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(
                            exception,
                            "Retry {RetryCount} of {MaxRetries} after {RetrySeconds}s delay due to: {ErrorMessage}",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalSeconds,
                            exception.Message);
                    }
                );
        }

        public static AsyncRetryPolicy CreateDeadlockRetryPolicy(ILogger logger, int maxRetries = 5)
        {
            return Policy
                .Handle<SqlException>(ex => ex.Number == 1205) // Only retry deadlock errors
                .WaitAndRetryAsync(
                    maxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt), // Linear backoff
                    (exception, timeSpan, retryCount, context) =>
                    {
                        logger.LogWarning(
                            "Deadlock detected. Retry {RetryCount} of {MaxRetries} after {RetryMilliseconds}ms delay",
                            retryCount,
                            maxRetries,
                            timeSpan.TotalMilliseconds);
                    }
                );
        }
    }
}