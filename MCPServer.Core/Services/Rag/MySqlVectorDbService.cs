using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Rag
{
    public class MySqlVectorDbService : IVectorDbService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<MySqlVectorDbService> _logger;
        private readonly IEmbeddingService _embeddingService;
        private readonly IDocumentService _documentService;

        public MySqlVectorDbService(
            McpServerDbContext dbContext,
            ILogger<MySqlVectorDbService> logger,
            IEmbeddingService embeddingService,
            IDocumentService documentService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _embeddingService = embeddingService;
            _documentService = documentService;
        }

        public async Task<bool> AddChunkAsync(Chunk chunk)
        {
            try
            {
                // Generate embedding if not already present
                if (chunk.Embedding == null || chunk.Embedding.Count == 0)
                {
                    chunk.Embedding = await _embeddingService.GetEmbeddingAsync(chunk.Content);
                }

                _dbContext.Chunks.Add(chunk);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Added chunk with ID: {ChunkId}", chunk.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding chunk with ID: {ChunkId}", chunk.Id);
                return false;
            }
        }

        public async Task<bool> AddChunksAsync(List<Chunk> chunks)
        {
            try
            {
                // Generate embeddings for chunks without embeddings
                var chunksToEmbed = chunks
                    .Where(c => c.Embedding == null || c.Embedding.Count == 0)
                    .ToList();

                if (chunksToEmbed.Count > 0)
                {
                    var texts = chunksToEmbed.Select(c => c.Content).ToList();
                    var embeddings = await _embeddingService.GetEmbeddingsAsync(texts);

                    for (int i = 0; i < chunksToEmbed.Count; i++)
                    {
                        if (i < embeddings.Count)
                        {
                            chunksToEmbed[i].Embedding = embeddings[i];
                        }
                    }
                }

                // Add all chunks to the database
                await _dbContext.Chunks.AddRangeAsync(chunks);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Added {ChunkCount} chunks", chunks.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding chunks");
                return false;
            }
        }

        public async Task<Chunk?> GetChunkAsync(string id)
        {
            return await _dbContext.Chunks.FindAsync(id);
        }

        public async Task<List<Chunk>> GetChunksByDocumentIdAsync(string documentId)
        {
            return await _dbContext.Chunks
                .Where(c => c.DocumentId == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();
        }

        public async Task<bool> UpdateChunkAsync(Chunk chunk)
        {
            try
            {
                var existingChunk = await _dbContext.Chunks.FindAsync(chunk.Id);

                if (existingChunk == null)
                {
                    _logger.LogWarning("Cannot update chunk. Chunk not found with ID: {ChunkId}", chunk.Id);
                    return false;
                }

                _dbContext.Entry(existingChunk).CurrentValues.SetValues(chunk);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Updated chunk with ID: {ChunkId}", chunk.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating chunk with ID: {ChunkId}", chunk.Id);
                return false;
            }
        }

        public async Task<bool> DeleteChunkAsync(string id)
        {
            try
            {
                var chunk = await _dbContext.Chunks.FindAsync(id);

                if (chunk == null)
                {
                    _logger.LogWarning("Cannot delete chunk. Chunk not found with ID: {ChunkId}", id);
                    return false;
                }

                _dbContext.Chunks.Remove(chunk);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted chunk with ID: {ChunkId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chunk with ID: {ChunkId}", id);
                return false;
            }
        }

        public async Task<bool> DeleteChunksByDocumentIdAsync(string documentId)
        {
            try
            {
                var chunks = await _dbContext.Chunks
                    .Where(c => c.DocumentId == documentId)
                    .ToListAsync();

                _dbContext.Chunks.RemoveRange(chunks);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted {ChunkCount} chunks for document ID: {DocumentId}", chunks.Count, documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting chunks for document ID: {DocumentId}", documentId);
                return false;
            }
        }

        public async Task<List<ChunkSearchResult>> SearchAsync(string query, int topK = 3, float minScore = 0.7f, Dictionary<string, string>? metadata = null)
        {
            try
            {
                // Get embedding for the query
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query);

                return await SearchByEmbeddingAsync(queryEmbedding, topK, minScore, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for query: {Query}", query);
                return new List<ChunkSearchResult>();
            }
        }

        public async Task<List<ChunkSearchResult>> SearchByEmbeddingAsync(List<float> embedding, int topK = 3, float minScore = 0.7f, Dictionary<string, string>? metadata = null)
        {
            try
            {
                var results = new List<ChunkSearchResult>();

                // Get all chunks from the database
                // Note: In a production environment, you would want to implement a more efficient search
                // using a vector database or specialized indexing
                var chunks = await _dbContext.Chunks.ToListAsync();

                // Filter chunks by metadata if provided
                if (metadata != null && metadata.Count > 0)
                {
                    // Since metadata is stored as JSON, we need to filter in memory
                    chunks = chunks.Where(c =>
                        metadata.All(m =>
                            c.Metadata.ContainsKey(m.Key) &&
                            c.Metadata[m.Key] == m.Value)).ToList();
                }

                // Calculate similarity scores
                foreach (var chunk in chunks)
                {
                    if (chunk.Embedding.Count > 0)
                    {
                        var score = _embeddingService.CalculateCosineSimilarity(embedding, chunk.Embedding);

                        if (score >= minScore)
                        {
                            var document = await _documentService.GetDocumentAsync(chunk.DocumentId);

                            results.Add(new ChunkSearchResult
                            {
                                Chunk = chunk,
                                Score = score,
                                Document = document ?? new Document { Id = chunk.DocumentId }
                            });
                        }
                    }
                }

                // Sort by score and take top K
                return results
                    .OrderByDescending(r => r.Score)
                    .Take(topK)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by embedding");
                return new List<ChunkSearchResult>();
            }
        }
    }
}
