using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Models.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MCPServer.Core.Features.Rag.Services.Interfaces;

namespace MCPServer.Core.Features.Rag.Services.Rag
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly LlmSettings _llmSettings;

        public EmbeddingService(
            HttpClient httpClient,
            IOptions<AppSettings> appSettings,
            ILogger<EmbeddingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _llmSettings = appSettings.Value.Llm;

            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _llmSettings.ApiKey);
        }

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

        public async Task<List<List<float>>> GetEmbeddingsAsync(List<string> texts)
        {
            try
            {
                var request = new EmbeddingRequest
                {
                    Model = "text-embedding-ada-002", // Use appropriate model
                    Input = texts
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var requestJson = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Use the embeddings endpoint
                var embeddingsEndpoint = "https://api.openai.com/v1/embeddings";
                var response = await _httpClient.PostAsync(embeddingsEndpoint, content);

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
}




