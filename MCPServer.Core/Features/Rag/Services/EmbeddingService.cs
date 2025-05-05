using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Features.Rag.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MCPServer.Core.Features.Rag.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly EmbeddingSettings _settings;

        public EmbeddingService(
            HttpClient httpClient,
            IOptions<EmbeddingSettings> settings,
            ILogger<EmbeddingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;

            // Configure the HTTP client
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Set the API key from settings
            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            }
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            try
            {
                _logger.LogInformation("Getting embedding for text of length {TextLength}", text.Length);

                // Prepare the request
                var request = new
                {
                    model = _settings.Model ?? "text-embedding-ada-002",
                    input = text
                };

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                // Send the request
                var response = await _httpClient.PostAsync("embeddings", content);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseString = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseString);

                if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
                {
                    _logger.LogError("No embedding data returned from API");
                    return Array.Empty<float>();
                }

                return embeddingResponse.Data[0].Embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding");
                throw;
            }
        }

        public async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
        {
            try
            {
                _logger.LogInformation("Getting embeddings for {Count} texts", texts.Count);

                var embeddings = new List<float[]>();

                // Process in batches to avoid rate limits
                int batchSize = 20;
                for (int i = 0; i < texts.Count; i += batchSize)
                {
                    var batch = texts.Skip(i).Take(batchSize).ToList();
                    var batchEmbeddings = await GetEmbeddingBatchAsync(batch);
                    embeddings.AddRange(batchEmbeddings);
                }

                return embeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embeddings for multiple texts");
                throw;
            }
        }

        private async Task<List<float[]>> GetEmbeddingBatchAsync(List<string> texts)
        {
            try
            {
                // Prepare the request
                var request = new
                {
                    model = _settings.Model ?? "text-embedding-ada-002",
                    input = texts
                };

                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

                // Send the request
                var response = await _httpClient.PostAsync("embeddings", content);
                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseString = await response.Content.ReadAsStringAsync();
                var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseString);

                if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
                {
                    _logger.LogError("No embedding data returned from API");
                    return new List<float[]>();
                }

                // Extract the embeddings
                return embeddingResponse.Data.Select(d => d.Embedding).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting embedding batch");
                throw;
            }
        }

        // Helper classes for JSON deserialization
        private class EmbeddingResponse
        {
            public List<EmbeddingData> Data { get; set; } = new List<EmbeddingData>();
        }

        private class EmbeddingData
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}
