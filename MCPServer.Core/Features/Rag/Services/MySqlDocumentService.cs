using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Features.Rag.Services.Interfaces;
using MCPServer.Core.Features.Shared.Services.Interfaces;
using MCPServer.Core.Models.Rag;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Features.Rag.Services
{
    public class MySqlDocumentService : IDocumentService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDbService _vectorDbService;
        private readonly ICachingService _cachingService;
        private readonly ILogger<MySqlDocumentService> _logger;

        public MySqlDocumentService(
            McpServerDbContext dbContext,
            IEmbeddingService embeddingService,
            IVectorDbService vectorDbService,
            ICachingService cachingService,
            ILogger<MySqlDocumentService> logger)
        {
            _dbContext = dbContext;
            _embeddingService = embeddingService;
            _vectorDbService = vectorDbService;
            _cachingService = cachingService;
            _logger = logger;
        }

        public async Task<Document> AddDocumentAsync(Document document, string content)
        {
            try
            {
                _logger.LogInformation("Adding document: {Title}", document.Title);

                // Set created timestamp
                document.CreatedAt = DateTime.UtcNow;

                // Add to database
                _dbContext.Documents.Add(document);
                await _dbContext.SaveChangesAsync();

                // Process the document content
                await ProcessDocumentContentAsync(document.Id, content);

                // Invalidate cache
                _cachingService.Remove("AllDocuments");

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding document: {Title}", document.Title);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(int id)
        {
            try
            {
                // Find the document
                var document = await _dbContext.Documents.FindAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Document with ID {DocumentId} not found for deletion", id);
                    return false;
                }

                // Delete all chunks for this document
                var chunks = await _dbContext.Chunks.Where(c => c.DocumentId == id).ToListAsync();
                _dbContext.Chunks.RemoveRange(chunks);

                // Delete the document
                _dbContext.Documents.Remove(document);
                await _dbContext.SaveChangesAsync();

                // Invalidate cache
                _cachingService.Remove("AllDocuments");
                _cachingService.Remove($"Document_{id}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID {DocumentId}", id);
                throw;
            }
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            try
            {
                // Try to get from cache first
                string cacheKey = "AllDocuments";
                if (_cachingService.TryGetValue(cacheKey, out List<Document> cachedDocuments))
                {
                    return cachedDocuments;
                }

                // If not in cache, get from database
                var documents = await _dbContext.Documents.ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, documents, TimeSpan.FromMinutes(5));

                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all documents");
                throw;
            }
        }

        public async Task<Document?> GetDocumentByIdAsync(int id)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Document_{id}";
                if (_cachingService.TryGetValue(cacheKey, out Document cachedDocument))
                {
                    return cachedDocument;
                }

                // If not in cache, get from database
                var document = await _dbContext.Documents.FindAsync(id);

                // Cache the result if found
                if (document != null)
                {
                    _cachingService.Set(cacheKey, document, TimeSpan.FromMinutes(5));
                }

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document with ID {DocumentId}", id);
                throw;
            }
        }

        public async Task<List<Chunk>> GetChunksForDocumentAsync(int documentId)
        {
            try
            {
                // Try to get from cache first
                string cacheKey = $"Chunks_Document_{documentId}";
                if (_cachingService.TryGetValue(cacheKey, out List<Chunk> cachedChunks))
                {
                    return cachedChunks;
                }

                // If not in cache, get from database
                var chunks = await _dbContext.Chunks
                    .Where(c => c.DocumentId == documentId)
                    .OrderBy(c => c.Order)
                    .ToListAsync();

                // Cache the result
                _cachingService.Set(cacheKey, chunks, TimeSpan.FromMinutes(5));

                return chunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chunks for document with ID {DocumentId}", documentId);
                throw;
            }
        }

        public async Task<bool> UpdateDocumentAsync(Document document, string? content = null)
        {
            try
            {
                // Find the document
                var existingDocument = await _dbContext.Documents.FindAsync(document.Id);
                if (existingDocument == null)
                {
                    _logger.LogWarning("Document with ID {DocumentId} not found for update", document.Id);
                    return false;
                }

                // Update properties
                existingDocument.Title = document.Title;
                existingDocument.Description = document.Description;
                existingDocument.Type = document.Type;
                existingDocument.Metadata = document.Metadata;
                existingDocument.UpdatedAt = DateTime.UtcNow;

                // Save changes
                await _dbContext.SaveChangesAsync();

                // Process new content if provided
                if (!string.IsNullOrEmpty(content))
                {
                    // Delete existing chunks
                    var chunks = await _dbContext.Chunks.Where(c => c.DocumentId == document.Id).ToListAsync();
                    _dbContext.Chunks.RemoveRange(chunks);
                    await _dbContext.SaveChangesAsync();

                    // Process the new content
                    await ProcessDocumentContentAsync(document.Id, content);
                }

                // Invalidate cache
                _cachingService.Remove("AllDocuments");
                _cachingService.Remove($"Document_{document.Id}");
                _cachingService.Remove($"Chunks_Document_{document.Id}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document with ID {DocumentId}", document.Id);
                throw;
            }
        }

        private async Task ProcessDocumentContentAsync(int documentId, string content)
        {
            try
            {
                _logger.LogInformation("Processing content for document ID {DocumentId}", documentId);

                // Split the content into chunks
                var chunks = SplitContentIntoChunks(content);
                _logger.LogInformation("Split content into {ChunkCount} chunks", chunks.Count);

                // Get embeddings for all chunks
                var chunkTexts = chunks.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GetEmbeddingsAsync(chunkTexts);

                // Create chunk objects
                var chunkObjects = new List<Chunk>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunk = new Chunk
                    {
                        DocumentId = documentId,
                        Content = chunks[i].Content,
                        Order = i,
                        Metadata = chunks[i].Metadata,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Store the embedding
                    if (i < embeddings.Count)
                    {
                        chunk.Embedding = JsonSerializer.Serialize(embeddings[i]);
                    }

                    chunkObjects.Add(chunk);
                }

                // Save chunks to database
                _dbContext.Chunks.AddRange(chunkObjects);
                await _dbContext.SaveChangesAsync();

                // Add to vector database
                await _vectorDbService.AddVectorsAsync(chunkObjects);

                _logger.LogInformation("Processed and saved {ChunkCount} chunks for document ID {DocumentId}", chunkObjects.Count, documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing content for document ID {DocumentId}", documentId);
                throw;
            }
        }

        private List<ChunkData> SplitContentIntoChunks(string content)
        {
            // Simple chunking strategy - split by paragraphs and combine small paragraphs
            var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<ChunkData>();
            var currentChunk = new StringBuilder();
            var currentMetadata = new Dictionary<string, string>();

            int chunkIndex = 0;
            foreach (var paragraph in paragraphs)
            {
                var trimmedParagraph = paragraph.Trim();
                if (string.IsNullOrEmpty(trimmedParagraph))
                {
                    continue;
                }

                // If adding this paragraph would make the chunk too large, save the current chunk and start a new one
                if (currentChunk.Length + trimmedParagraph.Length > 1000 && currentChunk.Length > 0)
                {
                    chunks.Add(new ChunkData
                    {
                        Content = currentChunk.ToString().Trim(),
                        Metadata = JsonSerializer.Serialize(currentMetadata)
                    });

                    currentChunk.Clear();
                    currentMetadata = new Dictionary<string, string>();
                    chunkIndex++;
                }

                // Add the paragraph to the current chunk
                if (currentChunk.Length > 0)
                {
                    currentChunk.AppendLine();
                    currentChunk.AppendLine();
                }
                currentChunk.Append(trimmedParagraph);

                // Add metadata
                currentMetadata["chunk_index"] = chunkIndex.ToString();
            }

            // Add the last chunk if it's not empty
            if (currentChunk.Length > 0)
            {
                chunks.Add(new ChunkData
                {
                    Content = currentChunk.ToString().Trim(),
                    Metadata = JsonSerializer.Serialize(currentMetadata)
                });
            }

            return chunks;
        }

        private class ChunkData
        {
            public string Content { get; set; } = string.Empty;
            public string Metadata { get; set; } = "{}";
        }
    }
}
