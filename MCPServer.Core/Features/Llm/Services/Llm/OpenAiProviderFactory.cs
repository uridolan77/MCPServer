using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace MCPServer.Core.Features.Llm.Services.Llm
{
    /// <summary>
    /// Factory for creating OpenAI clients
    /// </summary>
    public class OpenAiProviderFactory : ILlmProviderFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public OpenAiProviderFactory(
            ILoggerFactory loggerFactory,
            IHttpClientFactory httpClientFactory)
        {
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
        }

        public string ProviderName => "OpenAI";

        public bool CanHandle(string providerName)
        {
            return providerName.Equals(ProviderName, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ILlmClient> CreateClientAsync(LlmProvider provider, LlmModel model, LlmProviderCredential credential)
        {
            var factoryLogger = _loggerFactory.CreateLogger<OpenAiProviderFactory>();

            // Extract API key from credential
            string apiKey = "";

            if (credential == null)
            {
                factoryLogger.LogWarning("Credential is null for OpenAI provider");
            }
            else
            {
                // First check if API key is directly stored in the ApiKey property
                if (!string.IsNullOrEmpty(credential.ApiKey))
                {
                    factoryLogger.LogInformation("Using API key from ApiKey property");
                    apiKey = credential.ApiKey;
                }
                // Otherwise try to extract from encrypted credentials
                else if (!string.IsNullOrEmpty(credential.EncryptedCredentials))
                {
                    try
                    {
                        var credentialsJson = Encoding.UTF8.GetString(
                            Convert.FromBase64String(credential.EncryptedCredentials));

                        var credentials = JsonSerializer.Deserialize<OpenAiCredentials>(
                            credentialsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (credentials != null)
                        {
                            apiKey = credentials.ApiKey;
                            factoryLogger.LogInformation("API key extracted from EncryptedCredentials");
                        }
                    }
                    catch (Exception ex)
                    {
                        factoryLogger.LogError(ex, "Error decoding OpenAI credentials");
                        throw new InvalidOperationException("Invalid OpenAI credentials", ex);
                    }
                }
                else
                {
                    factoryLogger.LogWarning("No API key found in credential");
                }
            }

            // If still no API key, try environment variable or fail
            if (string.IsNullOrEmpty(apiKey))
            {
                // Try to get from environment
                apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

                if (string.IsNullOrEmpty(apiKey))
                {
                    factoryLogger.LogError("API key is missing for OpenAI provider");
                    throw new InvalidOperationException("API key is missing for OpenAI provider");
                }
                else
                {
                    factoryLogger.LogInformation("Using API key from environment variable");
                }
            }

            // Create a new HttpClient for each LLM client instance
            var httpClient = _httpClientFactory.CreateClient("OpenAI");
            var clientLogger = _loggerFactory.CreateLogger<OpenAiClient>();

            return new OpenAiClient(
                apiKey: apiKey,
                endpoint: provider.ApiEndpoint,
                modelId: model.ModelId,
                logger: clientLogger,
                httpClient: httpClient);
        }

        private class OpenAiCredentials
        {
            public string ApiKey { get; set; } = string.Empty;
            public string Organization { get; set; } = string.Empty;
        }
    }
}
