using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using MCPServer.Core.Data;
using MCPServer.Core.Exceptions;
using MCPServer.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions globally and returning standardized error responses
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        // Reusable JsonSerializerOptions instance
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context, McpServerDbContext dbContext)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(context, ex, dbContext);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception, McpServerDbContext dbContext)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                // Map specific exceptions to status codes
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                InvalidOperationException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                LlmProviderAuthException => HttpStatusCode.Unauthorized,
                _ => HttpStatusCode.InternalServerError
            };

            context.Response.StatusCode = (int)statusCode;

            // Log the error to the database
            var errorLog = new ErrorLog
            {
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Source = exception.Source,
                RequestPath = context.Request.Path,
                RequestMethod = context.Request.Method,
                UserId = context.User.Identity?.IsAuthenticated == true
                    ? Guid.Parse(context.User.FindFirst("UserId")?.Value ?? string.Empty)
                    : null,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                dbContext.ErrorLogs.Add(errorLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // If logging to DB fails, at least log to the console
                _logger.LogError(ex, "Failed to log error to database");
            }

            // Create a standardized API response
            ApiResponse<object> response;

            // For LLM provider auth errors, include the detailed error information
            if (exception is LlmProviderAuthException authEx)
            {
                response = ApiResponse<object>.ErrorResponse(authEx.Message);

                // Add additional error details
                var errorDetails = new Dictionary<string, object>
                {
                    { "providerName", authEx.ProviderName },
                    { "errorDetails", authEx.ErrorDetails },
                    { "errorId", errorLog.Id }
                };

                response.Data = errorDetails;
            }
            else
            {
                // Standard error response for other exceptions
                response = ApiResponse<object>.ErrorResponse(
                    "An error occurred while processing your request",
                    new List<string> { exception.Message });

                // Add error ID for tracking
                response.Data = new { errorId = errorLog.Id };

                // Add detailed error information in development environment
                if (_environment.IsDevelopment())
                {
                    response.Errors.Add(exception.StackTrace ?? "No stack trace available");
                }
            }

            var jsonResponse = JsonSerializer.Serialize(response, _jsonOptions);

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}