using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Exceptions;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Llm.Services.Llm
{
    /// <summary>
    /// Implementation of ILlmClient for OpenAI using direct HttpClient calls
    /// </summary>
    public class OpenAiClient : ILlmClient
    {
        private readonly ILogger<OpenAiClient> _logger;
        private readonly string _modelId;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;

        public OpenAiClient(
            string apiKey,
            string endpoint,
            string modelId,
            ILogger<OpenAiClient> logger,
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
                // Since we're having compatibility issues with the Semantic Kernel API,
                // use the HttpClient directly for the API call, similar to the streaming approach
                request.Stream = false;

                // Preprocess the request to ensure system messages are properly formatted
                request = ProcessRequestMessages(request);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var requestJson = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Set the Authorization header with the API key
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);

                // Log the request for debugging
                _logger.LogInformation("Sending request to OpenAI API: {RequestJson}", requestJson);

                // Send request to the OpenAI API
                var response = await _httpClient.PostAsync(_endpoint, content);
                
                // Check for auth issues specifically
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI authentication failed. Status: 401 Unauthorized. Response: {Response}", responseBody);
                    throw new LlmProviderAuthException(
                        "OpenAI", 
                        "Authentication failed with OpenAI API. Please check your API key.", 
                        responseBody);
                }
                
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var llmResponse = JsonSerializer.Deserialize<LlmResponse>(responseJson, jsonOptions);

                if (llmResponse == null)
                {
                    throw new Exception("Failed to deserialize LLM response");
                }

                return llmResponse;
            }
            catch (LlmProviderAuthException)
            {
                // Let this specific exception propagate up
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending request to OpenAI");
                throw;
            }
        }

        public async Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived)
        {
            try
            {
                // Since we need more low-level control for streaming,
                // we use the HttpClient directly to manage the streaming data

                // Set streaming flag
                request.Stream = true;

                // Preprocess the request to ensure system messages are properly formatted
                request = ProcessRequestMessages(request);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var requestJson = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Set the Authorization header with the API key
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _apiKey);
                    
                // Log API request details (without showing the full key)
                string maskedKey = "sk-...";
                if (_apiKey.Length > 10)
                {
                    maskedKey = $"sk-...{_apiKey.Substring(_apiKey.Length - 6)}";
                }
                _logger.LogInformation("Sending API request to {Endpoint} using model {ModelId} with key {MaskedKey}", 
                    _endpoint, request.Model, maskedKey);
                
                // Log the full request JSON for debugging
                _logger.LogInformation("Streaming request to OpenAI API: {RequestJson}", requestJson);

                // Start processing the response as soon as headers are available
                HttpResponseMessage response;
                try 
                {
                    response = await _httpClient.PostAsync(_endpoint, content);
                    
                    // Check for auth issues specifically
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("OpenAI authentication failed. Status: 401 Unauthorized. Response: {Response}", responseBody);
                        
                        // Extract the error message from the JSON response if possible
                        string errorMessage = "Authentication failed with OpenAI API. Please check your API key.";
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
                        
                        // Send the error message back to the client with a special prefix that can be detected by the usage service
                        await onChunkReceived($"[ERROR_NO_BILLING]Error connecting to OpenAI: {errorMessage}", true);
                        
                        // Also throw the exception so it's properly logged
                        throw new LlmProviderAuthException("OpenAI", errorMessage, responseBody);
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
                    _logger.LogError(ex, "HTTP request to OpenAI failed with status code {StatusCode}", ex.StatusCode);
                    await onChunkReceived("[ERROR_NO_BILLING]I apologize, but there was an error connecting to the AI service: " + ex.Message, true);
                    return;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                var responseBuilder = new StringBuilder();
                string? line;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // SSE format: lines starting with "data: "
                    if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);

                        // Check for the end of the stream
                        if (data == "[DONE]")
                        {
                            await onChunkReceived(responseBuilder.ToString(), true);
                            break;
                        }

                        try
                        {
                            var chunk = JsonSerializer.Deserialize<LlmResponse>(data, jsonOptions);

                            if (chunk != null && chunk.Choices.Count > 0 && chunk.Choices[0].Delta != null)
                            {
                                var chunkContent = chunk.Choices[0].Delta?.Content;

                                if (!string.IsNullOrEmpty(chunkContent))
                                {
                                    responseBuilder.Append(chunkContent);
                                    await onChunkReceived(chunkContent, false);
                                }

                                // Check if this is the last chunk
                                if (chunk.Choices[0].FinishReason != null)
                                {
                                    await onChunkReceived(responseBuilder.ToString(), true);
                                    break;
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Error parsing streaming response chunk: {Data}", data);
                        }
                    }
                }
            }
            catch (LlmProviderAuthException)
            {
                // This is already handled earlier in the method
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming response from OpenAI");
                await onChunkReceived("[ERROR_NO_BILLING]I apologize, but there was an error processing your request: " + ex.Message, true);
            }
        }

        /// <summary>
        /// Processes request messages to ensure system messages are properly handled
        /// </summary>
        /// <param name="request">The original LLM request</param>
        /// <returns>The processed request with properly formatted messages</returns>
        private LlmRequest ProcessRequestMessages(LlmRequest request)
        {
            // Make a copy of the request to avoid modifying the original
            var processedRequest = new LlmRequest
            {
                Model = request.Model ?? _modelId,
                Temperature = request.Temperature,
                Max_tokens = request.Max_tokens,
                Stream = request.Stream,
                Tools = request.Tools
            };

            // Log all incoming messages for debugging
            _logger.LogInformation("Processing {MessageCount} messages for OpenAI format", request.Messages.Count);
            bool hasSystemMessage = false;

            foreach (var message in request.Messages)
            {
                _logger.LogDebug("Message role: {Role}, Content: {Content}", message.Role, 
                    message.Content.Length > 50 ? message.Content.Substring(0, 50) + "..." : message.Content);
                
                // Check if there's a system message
                if (message.Role.ToLower() == "system")
                {
                    hasSystemMessage = true;
                    _logger.LogInformation("Found system message: {SystemMessage}", 
                        message.Content.Length > 50 ? message.Content.Substring(0, 50) + "..." : message.Content);
                }
            }

            // For OpenAI, we include system messages as part of the message array
            // No special processing is needed, just keep all messages in their original form
            processedRequest.Messages = request.Messages.ToList();

            // Log whether a system message was found
            if (hasSystemMessage)
            {
                _logger.LogInformation("System message will be included in OpenAI request");
            }
            else
            {
                _logger.LogWarning("No system message found in the request");
            }

            return processedRequest;
        }
    }
}
