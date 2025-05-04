using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MCPServer.API.Hubs
{
    // Temporarily remove the [Authorize] attribute for diagnostic purposes
    public class McpHub : Hub
    {
        private readonly ILogger<McpHub> _logger;
        private readonly ILlmService _llmService;
        private readonly IContextService _contextService;
        private readonly ITokenManager _tokenManager;
        private readonly IUserService _userService;

        public McpHub(
            ILogger<McpHub> logger,
            ILlmService llmService,
            IContextService contextService,
            ITokenManager tokenManager,
            IUserService userService)
        {
            _logger = logger;
            _llmService = llmService;
            _contextService = contextService;
            _tokenManager = tokenManager;
            _userService = userService;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            _logger.LogInformation($"Hub OnConnectedAsync - User: {username ?? "Anonymous"}, ConnectionId: {Context.ConnectionId}");
            
            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation($"User authenticated: {username}");
                _logger.LogInformation($"User claims: {string.Join(", ", Context.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            }
            else
            {
                _logger.LogWarning($"User not authenticated! ConnectionId: {Context.ConnectionId}");
            }
            
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(McpRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.SessionId))
                {
                    await Clients.Caller.SendAsync("ReceiveError", "SessionId is required");
                    return;
                }

                if (string.IsNullOrEmpty(request.UserInput))
                {
                    await Clients.Caller.SendAsync("ReceiveError", "UserInput is required");
                    return;
                }

                // Get the current user
                var username = Context.User?.Identity?.Name;
                if (string.IsNullOrEmpty(username))
                {
                    await Clients.Caller.SendAsync("ReceiveError", "Authentication required");
                    return;
                }

                // Check if session belongs to the user or create a new association
                if (!await _userService.IsSessionOwnedByUserAsync(request.SessionId, username))
                {
                    await _userService.AddSessionToUserAsync(request.SessionId, username);
                }

                // Get session context
                var context = await _contextService.GetSessionContextAsync(request.SessionId);

                // Add metadata if provided
                if (request.Metadata != null && request.Metadata.Count > 0)
                {
                    foreach (var item in request.Metadata)
                    {
                        context.Metadata[item.Key] = item.Value;
                    }
                    await _contextService.SaveContextAsync(context);
                }

                // Add user information to metadata
                if (!context.Metadata.ContainsKey("username"))
                {
                    context.Metadata["username"] = username;
                    await _contextService.SaveContextAsync(context);
                }

                // Trim context to fit token limit
                var trimmedContext = _tokenManager.TrimContextToFitTokenLimit(context, 16000);

                // Convert to LLM messages
                var messages = _tokenManager.ConvertToLlmMessages(trimmedContext.Messages, request.UserInput);

                // Create LLM request
                var llmRequest = new LlmRequest
                {
                    Model = "gpt-3.5-turbo", // This should come from config
                    Messages = messages,
                    Temperature = 0.7,
                    Max_tokens = 2000,
                    Stream = request.Stream
                };

                if (request.Stream)
                {
                    // Handle streaming response
                    var fullResponse = new StringBuilder();

                    await _llmService.StreamResponseAsync(llmRequest, async (chunk, isComplete) =>
                    {
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            if (!isComplete)
                            {
                                fullResponse.Append(chunk);
                            }

                            await Clients.Caller.SendAsync("ReceiveMessage", new McpResponse
                            {
                                SessionId = request.SessionId,
                                Output = chunk,
                                IsComplete = isComplete,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        if (isComplete)
                        {
                            // Update context with the complete response
                            await _contextService.UpdateContextAsync(
                                request.SessionId,
                                request.UserInput,
                                fullResponse.ToString());
                        }
                    });
                }
                else
                {
                    // Handle non-streaming response
                    var response = await _llmService.SendRequestAsync(llmRequest);

                    if (response.Choices.Count > 0 && response.Choices[0].Message != null)
                    {
                        var content = response.Choices[0].Message?.Content ?? string.Empty;

                        // Update context
                        await _contextService.UpdateContextAsync(request.SessionId, request.UserInput, content);

                        // Send response to client
                        await Clients.Caller.SendAsync("ReceiveMessage", new McpResponse
                        {
                            SessionId = request.SessionId,
                            Output = content,
                            IsComplete = true,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        await Clients.Caller.SendAsync("ReceiveError", "Empty response from LLM API");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                await Clients.Caller.SendAsync("ReceiveError", "An error occurred while processing your request");
            }
        }
    }
}
