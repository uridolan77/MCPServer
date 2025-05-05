using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Features.Rag.Services.Interfaces;
using MCPServer.Core.Models.Rag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Rag.Services
{
    public class MySqlVectorDbService : IVectorDbService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<MySqlVectorDbService> _logger;

        public MySqlVectorDbService(
            McpServerDbContext dbContext,
            IEmbeddingService embeddingService,
            ILogger<MySqlVectorDbService> logger)
        {
            _dbContext = dbContext;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task AddVectorsAsync(List<Chunk> chunks)
        {
            try
            {
                _logger.LogInformation("Adding {ChunkCount} vectors to vector database", chunks.Count);

                // In MySQL implementation, the vectors are already stored in the Chunks table
                // No additional action needed as the embeddings are stored in the Embedding column
                
                _logger.LogInformation("Vectors added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vectors to vector database");
                throw;
            }
        }

        public async Task<List<SearchResult>> SearchAsync(string query, int limit = 5, List<int>? documentIds = null)
        {
            try
            {
                _logger.LogInformation("Searching for query: {Query}", query);

                // Get embedding for the query
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);
                if (queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("Failed to get embedding for query");
                    return new List<SearchResult>();
                }

                // Convert query embedding to JSON string for comparison
                var queryEmbeddingJson = JsonSerializer.Serialize(queryEmbedding);

                // Get all chunks with embeddings
                var chunks = await _dbContext.Chunks
                    .Include(c => c.Document)
                    .Where(c => c.Embedding != null && c.Embedding.Length > 0)
                    .ToListAsync();

                if (documentIds != null && documentIds.Count > 0)
                {
                    chunks = chunks.Where(c => documentIds.Contains(c.DocumentId)).ToList();
                }

                // Calculate cosine similarity for each chunk
                var results = new List<SearchResult>();
                foreach (var chunk in chunks)
                {
                    try
                    {
                        var chunkEmbedding = JsonSerializer.Deserialize<float[]>(chunk.Embedding);
                        if (chunkEmbedding == null)
                        {
                            continue;
                        }

                        var similarity = CalculateCosineSimilarity(queryEmbedding, chunkEmbedding);

                        results.Add(new SearchResult
                        {
                            Chunk = chunk,
                            Document = chunk.Document,
                            Score = similarity
                        });
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning("Failed to deserialize embedding for chunk ID {ChunkId}", chunk.Id);
                    }
                }

                // Sort by similarity score and take the top results
                return results
                    .OrderByDescending(r => r.Score)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching vector database");
                throw;
            }
        }

        public async Task DeleteVectorsAsync(int documentId)
        {
            try
            {
                _logger.LogInformation("Deleting vectors for document ID {DocumentId}", documentId);

                // In MySQL implementation, the vectors are stored in the Chunks table
                // No additional action needed as the chunks will be deleted by the document service
                
                _logger.LogInformation("Vectors deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vectors for document ID {DocumentId}", documentId);
                throw;
            }
        }

        private float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            // Ensure vectors are of the same length
            if (vector1.Length != vector2.Length)
            {
                throw new ArgumentException("Vectors must be of the same length");
            }

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
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
