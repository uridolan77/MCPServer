using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Data;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Services.Interfaces;
using MCPServer.Core.Services.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MCPServer.Core.Services
{
    public class LlmService : ILlmService
    {
        private readonly ILogger<LlmService> _logger;
        private readonly LlmSettings _llmSettings;
        private readonly TokenSettings _tokenSettings;
        private readonly ITokenManager _tokenManager;
        private readonly McpServerDbContext _dbContext;
        private readonly IEnumerable<ILlmProviderFactory> _providerFactories;

        // Cache for the current LLM client
        private ILlmClient? _currentClient;
        private string _currentModelId = string.Empty;

        public LlmService(
            IOptions<LlmSettings> llmSettings,
            IOptions<TokenSettings> tokenSettings,
            ILogger<LlmService> logger,
            ITokenManager tokenManager,
            McpServerDbContext dbContext,
            IEnumerable<ILlmProviderFactory> providerFactories)
        {
            _logger = logger;
            _llmSettings = llmSettings.Value;
            _tokenSettings = tokenSettings.Value;
            _tokenManager = tokenManager;
            _dbContext = dbContext;
            _providerFactories = providerFactories;

            // Initialize the LLM client
            InitializeLlmClientAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeLlmClientAsync()
        {
            try
            {
                // Find the configured provider (from settings)
                var providerName = _llmSettings.Provider; // Using MCPServer.Core.Config.LlmSettings.Provider
                if (string.IsNullOrEmpty(providerName))
                {
                    providerName = "OpenAI"; // Default to OpenAI if not specified
                }

                // First check if we have any providers at all
                bool hasProviders = await _dbContext.LlmProviders.AnyAsync();

                if (!hasProviders)
                {
                    _logger.LogWarning("No LLM providers found in database. Creating default OpenAI provider.");

                    // Create a default OpenAI provider if none exists
                    var defaultProvider = new LlmProvider
                    {
                        Name = "OpenAI",
                        DisplayName = "OpenAI",
                        Description = "Default OpenAI provider added automatically",
                        ApiEndpoint = "https://api.openai.com/v1/chat/completions",
                        IsEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.LlmProviders.Add(defaultProvider);
                    await _dbContext.SaveChangesAsync();

                    // Add default model
                    var defaultModel = new LlmModel
                    {
                        ProviderId = defaultProvider.Id,
                        Name = "GPT-3.5 Turbo",
                        ModelId = "gpt-3.5-turbo",
                        Description = "Default OpenAI GPT-3.5 Turbo model added automatically",
                        MaxTokens = 4096,
                        IsEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.LlmModels.Add(defaultModel);

                    // Check for OpenAI API key in environment or settings
                    string? envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                    string apiKey = !string.IsNullOrEmpty(envApiKey) ? envApiKey : _llmSettings.ApiKey; // Using MCPServer.Core.Config.LlmSettings.ApiKey

                    if (string.IsNullOrEmpty(apiKey))
                    {
                        _logger.LogError("No OpenAI API key found. Please set OPENAI_API_KEY environment variable or configure in settings.");
                    }
                    else
                    {
                        // Add default credential
                        var defaultCredential = new LlmProviderCredential
                        {
                            ProviderId = defaultProvider.Id,
                            Name = "Default",
                            ApiKey = apiKey,
                            IsDefault = true,
                            IsEnabled = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _dbContext.LlmProviderCredentials.Add(defaultCredential);
                    }

                    await _dbContext.SaveChangesAsync();
                }

                // Now try to find the provider again (it should exist now if we had to create it)
                var provider = await _dbContext.LlmProviders
                    .Where(p => p.Name.ToLower() == providerName.ToLower() && p.IsEnabled)
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    _logger.LogWarning("Configured LLM provider '{ProviderName}' not found or not enabled. Falling back to any enabled provider.", providerName);

                    // Try to find any enabled provider as a fallback
                    provider = await _dbContext.LlmProviders
                        .Where(p => p.IsEnabled)
                        .FirstOrDefaultAsync();

                    if (provider == null)
                    {
                        _logger.LogError("No available LLM providers found. LLM services will not work.");
                        return;
                    }
                }

                // Find the configured model
                var modelId = _llmSettings.Model; // Using MCPServer.Core.Config.LlmSettings.Model
                if (string.IsNullOrEmpty(modelId))
                {
                    modelId = "gpt-3.5-turbo"; // Default model if not specified
                }

                // Check if we have any models for this provider
                bool hasModels = await _dbContext.LlmModels.AnyAsync(m => m.ProviderId == provider.Id);

                if (!hasModels)
                {
                    _logger.LogWarning("No models found for provider '{ProviderName}'. Creating default model.", provider.Name);

                    // Create a default model
                    var defaultModel = new LlmModel
                    {
                        ProviderId = provider.Id,
                        Name = "GPT-3.5 Turbo",
                        ModelId = "gpt-3.5-turbo",
                        Description = "Default model added automatically",
                        MaxTokens = 4096,
                        IsEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.LlmModels.Add(defaultModel);
                    await _dbContext.SaveChangesAsync();
                }

                var model = await _dbContext.LlmModels
                    .Where(m => m.ProviderId == provider.Id &&
                           m.IsEnabled &&
                           (m.ModelId == modelId || m.Name == modelId))
                    .FirstOrDefaultAsync();

                if (model == null)
                {
                    _logger.LogWarning("Configured model '{ModelId}' not found or not enabled. Using any available model.", modelId);
                    // Try to find any enabled model for this provider
                    model = await _dbContext.LlmModels
                        .Where(m => m.ProviderId == provider.Id && m.IsEnabled)
                        .FirstOrDefaultAsync();

                    if (model == null)
                    {
                        _logger.LogError("No available models found for provider '{ProviderName}'. LLM services will not work.", provider.Name);
                        return;
                    }
                }

                // Check for credentials
                bool hasCredentials = await _dbContext.LlmProviderCredentials.AnyAsync(c => c.ProviderId == provider.Id);

                if (!hasCredentials)
                {
                    _logger.LogWarning("No credentials found for provider '{ProviderName}'. Attempting to create default credential.", provider.Name);

                    // Check for OpenAI API key in environment or settings
                    string? envApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                    string apiKey = !string.IsNullOrEmpty(envApiKey) ? envApiKey : _llmSettings.ApiKey; // Using MCPServer.Core.Config.LlmSettings.ApiKey

                    if (string.IsNullOrEmpty(apiKey))
                    {
                        _logger.LogError("No API key found for provider '{ProviderName}'. LLM services will not work.", provider.Name);
                        return;
                    }

                    // Add default credential
                    var defaultCredential = new LlmProviderCredential
                    {
                        ProviderId = provider.Id,
                        Name = "Default",
                        ApiKey = apiKey,
                        IsDefault = true,
                        IsEnabled = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.LlmProviderCredentials.Add(defaultCredential);
                    await _dbContext.SaveChangesAsync();
                }

                var credential = await _dbContext.LlmProviderCredentials
                    .Where(c => c.ProviderId == provider.Id && c.IsEnabled && c.IsDefault)
                    .FirstOrDefaultAsync();

                if (credential == null)
                {
                    // Try any enabled credential if no default is found
                    credential = await _dbContext.LlmProviderCredentials
                        .Where(c => c.ProviderId == provider.Id && c.IsEnabled)
                        .FirstOrDefaultAsync();

                    if (credential == null)
                    {
                        _logger.LogError("No valid credentials found for provider '{ProviderName}'. LLM services will not work.", provider.Name);
                        return;
                    }
                }

                // Find the appropriate provider factory
                var factory = _providerFactories.FirstOrDefault(f => f.CanHandle(provider.Name));
                if (factory == null)
                {
                    _logger.LogError("No provider factory found for '{ProviderName}'. LLM services will not work.", provider.Name);
                    return;
                }

                // Create the client
                _currentClient = await factory.CreateClientAsync(provider, model, credential);
                _currentModelId = model.ModelId;
                _logger.LogInformation("Initialized LLM client for provider '{ProviderName}' with model '{ModelId}'",
                    provider.Name, model.ModelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize LLM client");
            }
        }

        public async Task<string> SendWithContextAsync(string userInput, SessionContext context, string? specificModelId = null)
        {
            try
            {
                // Trim context to fit token limit
                var trimmedContext = _tokenManager.TrimContextToFitTokenLimit(
                    context,
                    _tokenSettings.MaxContextTokens - _tokenSettings.MaxTokensPerMessage); // Using MCPServer.Core.Config.TokenSettings properties

                // Convert to LLM messages
                var messages = _tokenManager.ConvertToLlmMessages(trimmedContext.Messages, userInput);

                // Create LLM request
                var llmRequest = new LlmRequest
                {
                    Model = specificModelId ?? _currentModelId,
                    Messages = messages,
                    Temperature = _llmSettings.Temperature, // Using MCPServer.Core.Config.LlmSettings.Temperature
                    Max_tokens = _llmSettings.MaxTokens, // Using MCPServer.Core.Config.LlmSettings.MaxTokens
                    Stream = false
                };

                // Use SendRequestAsync which now handles specific model requests
                var response = await SendRequestAsync(llmRequest);

                if (response.Choices.Count > 0 && response.Choices[0].Message != null)
                {
                    return response.Choices[0].Message?.Content ?? string.Empty;
                }

                _logger.LogWarning("Empty response from LLM API");
                return "I apologize, but I couldn't generate a response at this time.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to LLM API");
                return "I apologize, but there was an error processing your request.";
            }
        }

        public async Task<LlmResponse> SendRequestAsync(LlmRequest request)
        {
            // Check if a specific model is requested
            if (!string.IsNullOrEmpty(request.Model) && request.Model != _currentModelId)
            {
                _logger.LogInformation("Using specific model for request: {ModelId}", request.Model);

                // Create a client for the specific model
                var client = await CreateClientForModelIdAsync(request.Model);

                if (client != null)
                {
                    // Use the specific client for this request
                    return await client.SendRequestAsync(request);
                }
                else
                {
                    _logger.LogWarning("Failed to create client for model {ModelId}, falling back to default client", request.Model);
                }
            }

            // If no specific model requested or client creation failed, use the default client
            if (_currentClient == null)
            {
                await InitializeLlmClientAsync();
                if (_currentClient == null)
                {
                    throw new InvalidOperationException("No LLM client available");
                }
            }

            // Only override the model if none was specified
            if (string.IsNullOrEmpty(request.Model))
            {
                request.Model = _currentModelId;
            }

            return await _currentClient.SendRequestAsync(request);
        }

        public async Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived)
        {
            try
            {
                // Check if a specific model is requested
                if (!string.IsNullOrEmpty(request.Model) && request.Model != _currentModelId)
                {
                    _logger.LogInformation("Using specific model for request: {ModelId}", request.Model);

                    // Create a client for the specific model
                    var client = await CreateClientForModelIdAsync(request.Model);

                    if (client != null)
                    {
                        // Use the specific client for this request
                        await client.StreamResponseAsync(request, onChunkReceived);
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create client for model {ModelId}, falling back to default client", request.Model);
                    }
                }

                // If no specific model requested or client creation failed, use the default client
                if (_currentClient == null)
                {
                    await InitializeLlmClientAsync();
                    if (_currentClient == null)
                    {
                        _logger.LogError("No LLM client available");
                        await onChunkReceived("I apologize, but the LLM service is not properly configured.", true);
                        return;
                    }
                }

                // Only override the model if none was specified
                if (string.IsNullOrEmpty(request.Model))
                {
                    request.Model = _currentModelId;
                }

                await _currentClient.StreamResponseAsync(request, onChunkReceived);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming response from LLM API");
                await onChunkReceived("I apologize, but there was an error processing your request.", true);
            }
        }

        /// <summary>
        /// Creates a client for a specific model ID
        /// </summary>
        private async Task<ILlmClient?> CreateClientForModelIdAsync(string modelId)
        {
            try
            {
                _logger.LogInformation("Creating client for model ID: {ModelId}", modelId);

                // Find the model in the database
                var model = await _dbContext.LlmModels
                    .Where(m => m.ModelId == modelId && m.IsEnabled)
                    .FirstOrDefaultAsync();

                if (model == null)
                {
                    _logger.LogWarning("Model with ID {ModelId} not found or not enabled", modelId);
                    return null;
                }

                // Get the provider for this model
                var provider = await _dbContext.LlmProviders
                    .Where(p => p.Id == model.ProviderId && p.IsEnabled)
                    .FirstOrDefaultAsync();

                if (provider == null)
                {
                    _logger.LogWarning("Provider for model {ModelId} not found or not enabled", modelId);
                    return null;
                }

                _logger.LogInformation("Found provider {ProviderName} for model {ModelId}", provider.Name, modelId);

                // Get credentials for this provider
                var credential = await _dbContext.LlmProviderCredentials
                    .Where(c => c.ProviderId == provider.Id && c.IsEnabled && c.IsDefault)
                    .FirstOrDefaultAsync();

                if (credential == null)
                {
                    // Try any enabled credential if no default is found
                    credential = await _dbContext.LlmProviderCredentials
                        .Where(c => c.ProviderId == provider.Id && c.IsEnabled)
                        .FirstOrDefaultAsync();

                    if (credential == null)
                    {
                        _logger.LogError("No valid credentials found for provider '{ProviderName}'", provider.Name);
                        return null;
                    }
                }

                // Find the appropriate provider factory
                var factory = _providerFactories.FirstOrDefault(f => f.CanHandle(provider.Name));
                if (factory == null)
                {
                    _logger.LogError("No provider factory found for '{ProviderName}'", provider.Name);
                    return null;
                }

                // Create the client
                var client = await factory.CreateClientAsync(provider, model, credential);
                _logger.LogInformation("Created client for provider '{ProviderName}' with model '{ModelId}'",
                    provider.Name, model.ModelId);

                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating client for model {ModelId}", modelId);
                return null;
            }
        }
    }
}
