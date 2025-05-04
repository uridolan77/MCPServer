using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Data.Repositories;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Rag
{
    public class VectorDbService : IVectorDbService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChunkRepository _chunkRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<VectorDbService> _logger;
        private readonly IEmbeddingService _embeddingService;

        public VectorDbService(
            IUnitOfWork unitOfWork,
            IChunkRepository chunkRepository,
            IDocumentRepository documentRepository,
            IEmbeddingService embeddingService,
            ILogger<VectorDbService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                await _chunkRepository.AddAsync(chunk);
                await _unitOfWork.SaveChangesAsync();

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
                await _unitOfWork.BeginTransactionAsync();

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
                foreach (var chunk in chunks)
                {
                    await _chunkRepository.AddAsync(chunk);
                }
                
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Added {ChunkCount} chunks", chunks.Count);
                return true;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error adding chunks");
                return false;
            }
        }

        public async Task<Chunk?> GetChunkAsync(string id)
        {
            return await _chunkRepository.GetByIdAsync(id);
        }

        public async Task<List<Chunk>> GetChunksByDocumentIdAsync(string documentId)
        {
            var chunks = await _chunkRepository.GetChunksByDocumentIdAsync(documentId);
            return chunks.ToList();
        }

        public async Task<bool> UpdateChunkAsync(Chunk chunk)
        {
            try
            {
                var existingChunk = await _chunkRepository.GetByIdAsync(chunk.Id);

                if (existingChunk == null)
                {
                    _logger.LogWarning("Cannot update chunk. Chunk not found with ID: {ChunkId}", chunk.Id);
                    return false;
                }

                // Generate embedding if content has changed
                if (existingChunk.Content != chunk.Content || chunk.Embedding == null || chunk.Embedding.Count == 0)
                {
                    chunk.Embedding = await _embeddingService.GetEmbeddingAsync(chunk.Content);
                }

                await _chunkRepository.UpdateAsync(chunk);
                await _unitOfWork.SaveChangesAsync();

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
                var chunk = await _chunkRepository.GetByIdAsync(id);

                if (chunk == null)
                {
                    _logger.LogWarning("Cannot delete chunk. Chunk not found with ID: {ChunkId}", id);
                    return false;
                }

                await _chunkRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();

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
            return await _chunkRepository.DeleteChunksByDocumentIdAsync(documentId);
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

                // Get chunks filtered by metadata if provided
                IEnumerable<Chunk> chunks;
                if (metadata != null && metadata.Count > 0)
                {
                    chunks = await _chunkRepository.GetChunksByMetadataAsync(metadata);
                }
                else
                {
                    chunks = await _chunkRepository.GetAllAsync();
                }

                // Calculate similarity scores
                foreach (var chunk in chunks)
                {
                    if (chunk.Embedding.Count > 0)
                    {
                        var score = _embeddingService.CalculateCosineSimilarity(embedding, chunk.Embedding);

                        if (score >= minScore)
                        {
                            var document = await _documentRepository.GetByIdAsync(chunk.DocumentId);

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
