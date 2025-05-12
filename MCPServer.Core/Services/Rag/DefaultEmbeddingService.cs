using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MCPServer.Core.Services.Rag
{
    /// <summary>
    /// Default implementation of the embedding service that provides vector embeddings for text.
    /// </summary>
    public class DefaultEmbeddingService : IEmbeddingService
    {
        private readonly ILogger<DefaultEmbeddingService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly LlmSettings _llmSettings;
        private const string DEFAULT_EMBEDDING_MODEL = "text-embedding-ada-002";
        private const string DEFAULT_EMBEDDING_ENDPOINT = "https://api.openai.com/v1/embeddings";

        public DefaultEmbeddingService(
            IHttpClientFactory httpClientFactory,
            IOptions<AppSettings> appSettings,
            ILogger<DefaultEmbeddingService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _llmSettings = appSettings?.Value?.Llm ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <summary>
        /// Get embedding for a single text
        /// </summary>
        public async Task<List<float>> GetEmbeddingAsync(string text)
        {
            try
            {
                var embeddings = await GetEmbeddingsAsync(new List<string> { text });
                return embeddings.Count > 0 ? embeddings[0] : new List<float>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding for text");
                return new List<float>();
            }
        }

        /// <summary>
        /// Get embeddings for multiple texts
        /// </summary>
        public async Task<List<List<float>>> GetEmbeddingsAsync(List<string> texts)
        {
            try
            {
                _logger.LogInformation("Getting embeddings for {Count} texts", texts.Count);

                var httpClient = _httpClientFactory.CreateClient();
                
                // Configure HttpClient
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _llmSettings.ApiKey);

                // Create embedding request
                var request = new EmbeddingRequest
                {
                    Input = texts,
                    Model = DEFAULT_EMBEDDING_MODEL // Using default model since LlmSettings doesn't have EmbeddingModel
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send request to API
                var response = await httpClient.PostAsync(DEFAULT_EMBEDDING_ENDPOINT, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseJson, jsonOptions);

                if (embeddingResponse == null || embeddingResponse.Data.Count == 0)
                {
                    _logger.LogWarning("Empty embedding response from API");
                    return new List<List<float>>();
                }

                var embeddings = new List<List<float>>();
                foreach (var data in embeddingResponse.Data)
                {
                    embeddings.Add(data.Embedding);
                }

                return embeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embeddings for texts");
                return new List<List<float>>();
            }
        }

        /// <summary>
        /// Calculate cosine similarity between two embeddings
        /// </summary>
        public float CalculateCosineSimilarity(List<float> embedding1, List<float> embedding2)
        {
            if (embedding1.Count != embedding2.Count)
            {
                throw new ArgumentException("Embeddings must have the same dimension");
            }

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < embedding1.Count; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += embedding1[i] * embedding1[i];
                magnitude2 += embedding2[i] * embedding2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }
    }

    public class EmbeddingRequest
    {
        public List<string> Input { get; set; } = new List<string>();
        public string Model { get; set; } = string.Empty;
    }
}