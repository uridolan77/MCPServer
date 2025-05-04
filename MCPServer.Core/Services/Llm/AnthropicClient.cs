using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MCPServer.Core.Exceptions;
using MCPServer.Core.Models;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Llm
{
    /// <summary>
    /// Implementation of ILlmClient for Anthropic using direct HttpClient calls
    /// </summary>
    public class AnthropicClient : ILlmClient
    {
        private readonly ILogger<AnthropicClient> _logger;
        private readonly string _modelId;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _anthropicVersion = "2023-06-01"; // Latest API version for Claude 3 models

        public AnthropicClient(
            string apiKey,
            string endpoint,
            string modelId,
            ILogger<AnthropicClient> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _apiKey = apiKey;
            _endpoint = endpoint;
            _modelId = modelId;
            _httpClient = httpClient;
        }

        public async Task<LlmResponse> SendRequestAsync(LlmRequest request)
        {
            try
            {
                // Convert the standard LlmRequest to Anthropic format
                var anthropicRequest = ConvertToAnthropicRequest(request);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var requestJson = JsonSerializer.Serialize(anthropicRequest, jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Set the required Anthropic headers
                _httpClient.DefaultRequestHeaders.Clear();
                // Try direct x-api-key header
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", _anthropicVersion);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Log the request for debugging
                _logger.LogInformation("Sending request to Anthropic API: {RequestJson}", requestJson);

                // Send request to the Anthropic API
                var response = await _httpClient.PostAsync(_endpoint, content);

                // Check for auth issues specifically
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Anthropic authentication failed. Status: 401 Unauthorized. Response: {Response}", responseBody);
                    throw new LlmProviderAuthException(
                        "Anthropic",
                        "Authentication failed with Anthropic API. Please check your API key.",
                        responseBody);
                }

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, jsonOptions);

                if (anthropicResponse == null)
                {
                    throw new Exception("Failed to deserialize Anthropic response");
                }

                // Convert Anthropic response to standard LlmResponse
                return ConvertFromAnthropicResponse(anthropicResponse);
            }
            catch (LlmProviderAuthException)
            {
                // Let this specific exception propagate up
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request to Anthropic");
                throw;
            }
        }

        public async Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived)
        {
            try
            {
                // Convert the standard LlmRequest to Anthropic format
                var anthropicRequest = ConvertToAnthropicRequest(request);

                // Set streaming flag
                anthropicRequest.Stream = true;

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var requestJson = JsonSerializer.Serialize(anthropicRequest, jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Set the required Anthropic headers
                _httpClient.DefaultRequestHeaders.Clear();
                // Try direct x-api-key header
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", _anthropicVersion);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Log the request for debugging
                _logger.LogInformation("Sending streaming request to Anthropic API: {RequestJson}", requestJson);

                // Log API request details (without showing the full key)
                string maskedKey = "sk-...";
                if (_apiKey.Length > 10)
                {
                    maskedKey = $"sk-...{_apiKey.Substring(_apiKey.Length - 6)}";
                }
                _logger.LogInformation("Sending API request to {Endpoint} using model {ModelId} with key {MaskedKey}",
                    _endpoint, anthropicRequest.Model, maskedKey);

                // Start processing the response as soon as headers are available
                HttpResponseMessage? response = null;
                try
                {
                    response = await _httpClient.PostAsync(_endpoint, content);

                    // Check for auth issues specifically
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Anthropic authentication failed. Status: 401 Unauthorized. Response: {Response}", responseBody);

                        // Extract the error message from the JSON response if possible
                        string errorMessage = "Authentication failed with Anthropic API. Please check your API key.";
                        try
                        {
                            var errorJson = JsonDocument.Parse(responseBody);
                            if (errorJson.RootElement.TryGetProperty("error", out var errorElement) &&
                                errorElement.TryGetProperty("message", out var messageElement))
                            {
                                errorMessage = messageElement.GetString() ?? errorMessage;
                            }
                        }
                        catch {}

                        // Send the error message back to the client
                        await onChunkReceived($"Error connecting to Anthropic: {errorMessage}", true);

                        // Also throw the exception so it's properly logged
                        throw new LlmProviderAuthException("Anthropic", errorMessage, responseBody);
                    }

                    response.EnsureSuccessStatusCode();
                }
                catch (LlmProviderAuthException)
                {
                    // We've already handled this with onChunkReceived
                    return;
                }
                catch (HttpRequestException ex)
                {
                    // When we receive a Bad Request (400), try to extract the error message from the response
                    if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        try
                        {
                            // Ensure response variable is defined and accessible in this scope
                            if (response != null)
                            {
                                string responseBody = await response.Content.ReadAsStringAsync();
                                _logger.LogError("Anthropic API error (400 Bad Request): {Response}", responseBody);
                                
                                // Try to parse error details from the response body
                                var errorResponse = JsonDocument.Parse(responseBody);
                                string errorMessage = "Unknown error";
                                
                                if (errorResponse.RootElement.TryGetProperty("error", out var errorElement))
                                {
                                    if (errorElement.TryGetProperty("message", out var messageElement))
                                    {
                                        errorMessage = messageElement.GetString() ?? errorMessage;
                                    }
                                    else if (errorElement.ValueKind == JsonValueKind.String)
                                    {
                                        errorMessage = errorElement.GetString() ?? errorMessage;
                                    }
                                }
                                
                                await onChunkReceived($"Error connecting to Anthropic: {errorMessage}", true);
                            }
                            else
                            {
                                await onChunkReceived("Error connecting to Anthropic API: Bad Request", true);
                            }
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogError(parseEx, "Failed to parse Anthropic error response");
                            await onChunkReceived("I apologize, but there was an error connecting to the AI service: " + ex.Message, true);
                        }
                    }
                    else
                    {
                        _logger.LogError(ex, "HTTP request to Anthropic failed with status code {StatusCode}", ex.StatusCode);
                        await onChunkReceived("I apologize, but there was an error connecting to the AI service: " + ex.Message, true);
                    }
                    return;
                }

                // Process the streaming response
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        var buffer = new StringBuilder();
                        string? line;

                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            // Log the raw line for debugging
                            _logger.LogDebug("Received streaming line: {Line}", line);

                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            if (line.StartsWith("data: "))
                            {
                                string jsonData = line.Substring("data: ".Length);

                                // Check for the [DONE] marker
                                if (jsonData == "[DONE]")
                                {
                                    _logger.LogDebug("Received [DONE] marker");
                                    await onChunkReceived("", true);
                                    break;
                                }

                                try
                                {
                                    // Log the JSON data for debugging
                                    _logger.LogDebug("Parsing JSON data: {JsonData}", jsonData);

                                    var chunkResponse = JsonSerializer.Deserialize<AnthropicStreamResponse>(jsonData, jsonOptions);

                                    if (chunkResponse != null)
                                    {
                                        _logger.LogDebug("Received chunk type: {Type}", chunkResponse.Type);

                                        if (chunkResponse.Type == "content_block_delta")
                                        {
                                            string chunkText = chunkResponse.Delta?.Text ?? "";
                                            bool isComplete = chunkResponse.StopReason != null;

                                            _logger.LogDebug("Content delta: {Text}, IsComplete: {IsComplete}",
                                                chunkText, isComplete);

                                            await onChunkReceived(chunkText, isComplete);
                                        }
                                        else if (chunkResponse.Type == "content_block_start")
                                        {
                                            // Just log this event
                                            _logger.LogDebug("Content block start received");
                                        }
                                        else if (chunkResponse.Type == "message_start")
                                        {
                                            // Just log this event
                                            _logger.LogDebug("Message start received");
                                        }
                                        else if (chunkResponse.Type == "message_delta")
                                        {
                                            // Just log this event
                                            _logger.LogDebug("Message delta received");
                                        }
                                        else if (chunkResponse.Type == "message_stop")
                                        {
                                            // End of message
                                            _logger.LogDebug("Message stop received");
                                            await onChunkReceived("", true);
                                            break;
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Unknown chunk type: {Type}", chunkResponse.Type);
                                        }
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    _logger.LogError(ex, "Error parsing Anthropic streaming response: {JsonData}", jsonData);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Unexpected streaming line format: {Line}", line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming response from Anthropic");
                await onChunkReceived("I apologize, but there was an error processing your request: " + ex.Message, true);
            }
        }

        private AnthropicRequest ConvertToAnthropicRequest(LlmRequest request)
        {
            // Create a new Anthropic request
            var anthropicRequest = new AnthropicRequest
            {
                Model = request.Model ?? _modelId,
                MaxTokens = request.Max_tokens > 0 ? request.Max_tokens : 1000,
                Temperature = request.Temperature,
                Stream = request.Stream
            };

            // Extract system message if present
            string? systemMessage = null;
            var filteredMessages = new List<LlmMessage>();
            
            // Log all incoming messages for debugging
            _logger.LogInformation("Converting {MessageCount} messages to Anthropic format", request.Messages.Count);
            foreach (var message in request.Messages)
            {
                _logger.LogDebug("Message role: {Role}, Content: {Content}", message.Role, 
                    message.Content.Length > 50 ? message.Content.Substring(0, 50) + "..." : message.Content);
            }

            foreach (var message in request.Messages)
            {
                if (message.Role.ToLower() == "system")
                {
                    // Save the system message
                    systemMessage = message.Content;
                    _logger.LogInformation("Found system message: {SystemMessage}", 
                        systemMessage.Length > 50 ? systemMessage.Substring(0, 50) + "..." : systemMessage);
                }
                else
                {
                    // Keep non-system messages
                    filteredMessages.Add(message);
                }
            }

            // Set system message if found
            if (!string.IsNullOrEmpty(systemMessage))
            {
                anthropicRequest.System = systemMessage;
                _logger.LogInformation("Using system prompt for Anthropic request: {SystemPrompt}", 
                    systemMessage.Length > 50 ? systemMessage.Substring(0, 50) + "..." : systemMessage);
            }
            else
            {
                _logger.LogWarning("No system message found in the request");
            }

            // Convert messages to Anthropic format
            var messages = new List<AnthropicMessage>();

            // Ensure we have at least one user message
            bool hasUserMessage = false;

            foreach (var message in filteredMessages)
            {
                // Map standard roles to Anthropic roles
                string role = message.Role.ToLower() switch
                {
                    "user" => "user",
                    "assistant" => "assistant",
                    _ => "user" // Default to user for unknown roles
                };

                if (role == "user") hasUserMessage = true;

                // For content, ensure it's not null or empty
                string content = !string.IsNullOrEmpty(message.Content)
                    ? message.Content
                    : (role == "user" ? "Hello" : "How can I help you?");

                if (role == "user" || role == "assistant")
                {
                    // Create content object for user and assistant messages
                    var contentObj = new[]
                    {
                        new { type = "text", text = content }
                    };

                    messages.Add(new AnthropicMessage
                    {
                        Role = role,
                        Content = contentObj
                    });
                }
            }

            // If no user message, add a default one
            if (!hasUserMessage)
            {
                var defaultContent = new[] { new { type = "text", text = "Hello" } };
                messages.Add(new AnthropicMessage
                {
                    Role = "user",
                    Content = defaultContent
                });
            }

            anthropicRequest.Messages = messages;

            // Log the converted request
            _logger.LogInformation("Converted request to Anthropic format: Model={Model}, MaxTokens={MaxTokens}, Messages={MessageCount}",
                anthropicRequest.Model, anthropicRequest.MaxTokens, anthropicRequest.Messages.Count);

            return anthropicRequest;
        }

        private LlmResponse ConvertFromAnthropicResponse(AnthropicResponse anthropicResponse)
        {
            // Create a standard LlmResponse from the Anthropic response
            var response = new LlmResponse
            {
                Id = anthropicResponse.Id,
                Object = "chat.completion", // Match OpenAI format
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = anthropicResponse.Model
            };

            // Create a choice with the message content
            var choice = new LlmChoice
            {
                Index = 0,
                Message = new LlmResponseMessage
                {
                    Role = "assistant",
                    Content = anthropicResponse.Content.FirstOrDefault()?.Text ?? string.Empty
                },
                FinishReason = anthropicResponse.StopReason
            };

            response.Choices.Add(choice);

            // Add usage information if available
            if (anthropicResponse.Usage != null)
            {
                response.Usage = new LlmUsage
                {
                    PromptTokens = anthropicResponse.Usage.InputTokens,
                    CompletionTokens = anthropicResponse.Usage.OutputTokens,
                    TotalTokens = anthropicResponse.Usage.InputTokens + anthropicResponse.Usage.OutputTokens
                };
            }

            return response;
        }

        #region Anthropic API Models

        private class AnthropicRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("messages")]
            public List<AnthropicMessage> Messages { get; set; } = new List<AnthropicMessage>();

            [JsonPropertyName("system")]
            public string? System { get; set; }

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; } = 1000;

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; } = 0.7;

            [JsonPropertyName("stream")]
            public bool Stream { get; set; } = false;
        }

        private class AnthropicMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public object Content { get; set; } = string.Empty;
        }

        private class AnthropicResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public List<AnthropicContent> Content { get; set; } = new List<AnthropicContent>();

            [JsonPropertyName("stop_reason")]
            public string? StopReason { get; set; }

            [JsonPropertyName("usage")]
            public AnthropicUsage? Usage { get; set; }
        }

        private class AnthropicContent
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private class AnthropicUsage
        {
            [JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            [JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }
        }

        private class AnthropicStreamResponse
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("delta")]
            public AnthropicDelta? Delta { get; set; }

            [JsonPropertyName("content_block")]
            public AnthropicContentBlock? ContentBlock { get; set; }

            [JsonPropertyName("stop_reason")]
            public string? StopReason { get; set; }

            [JsonPropertyName("message")]
            public AnthropicMessage? Message { get; set; }

            [JsonPropertyName("content_blocks")]
            public List<AnthropicContentBlock>? ContentBlocks { get; set; }
        }

        private class AnthropicDelta
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }

        private class AnthropicContentBlock
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        #endregion
    }
}
