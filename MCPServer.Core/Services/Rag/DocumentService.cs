using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Rag
{
    public class DocumentService : IDocumentService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(
            McpServerDbContext dbContext,
            ILogger<DocumentService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Document> AddDocumentAsync(Document document)
        {
            try
            {
                if (string.IsNullOrEmpty(document.Id))
                {
                    document.Id = Guid.NewGuid().ToString();
                }

                document.CreatedAt = DateTime.UtcNow;
                // Document doesn't have UpdatedAt property

                _dbContext.Documents.Add(document);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Added document: {DocumentTitle} (ID: {DocumentId})", 
                    document.Title, document.Id);

                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding document: {DocumentTitle}", document.Title);
                throw;
            }
        }

        public async Task<Document?> GetDocumentAsync(string id)
        {
            try
            {
                return await _dbContext.Documents.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document with ID: {DocumentId}", id);
                return null;
            }
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            try
            {
                return await _dbContext.Documents
                    .OrderByDescending(d => d.CreatedAt)  // Using CreatedAt instead of UpdatedAt
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all documents");
                return new List<Document>();
            }
        }

        public async Task<List<Document>> GetDocumentsByTagAsync(string tag)
        {
            try
            {
                // Filter by tags that contain the specified tag
                return await _dbContext.Documents
                    .Where(d => d.Tags.Contains(tag))
                    .OrderByDescending(d => d.CreatedAt)  // Using CreatedAt instead of UpdatedAt
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents with tag: {Tag}", tag);
                return new List<Document>();
            }
        }

        public async Task<bool> UpdateDocumentAsync(Document document)
        {
            try
            {
                var existingDocument = await _dbContext.Documents.FindAsync(document.Id);

                if (existingDocument == null)
                {
                    _logger.LogWarning("Cannot update document. Document not found with ID: {DocumentId}", document.Id);
                    return false;
                }

                // Document doesn't have UpdatedAt property
                // Keep original creation date
                document.CreatedAt = existingDocument.CreatedAt;
                
                _dbContext.Entry(existingDocument).CurrentValues.SetValues(document);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Updated document: {DocumentTitle} (ID: {DocumentId})", 
                    document.Title, document.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document with ID: {DocumentId}", document.Id);
                return false;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            try
            {
                var document = await _dbContext.Documents.FindAsync(id);

                if (document == null)
                {
                    _logger.LogWarning("Cannot delete document. Document not found with ID: {DocumentId}", id);
                    return false;
                }

                _dbContext.Documents.Remove(document);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Deleted document with ID: {DocumentId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID: {DocumentId}", id);
                return false;
            }
        }

        public async Task<List<Chunk>> ChunkDocumentAsync(Document document, int chunkSize = 1000, int chunkOverlap = 200)
        {
            try
            {
                _logger.LogInformation("Chunking document: {DocumentTitle} (ID: {DocumentId})", 
                    document.Title, document.Id);

                var chunks = new List<Chunk>();
                
                if (string.IsNullOrEmpty(document.Content))
                {
                    _logger.LogWarning("Document content is empty, cannot chunk document with ID: {DocumentId}", document.Id);
                    return chunks;
                }

                // Split the document content into paragraphs
                var paragraphs = document.Content
                    .Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();

                // Group paragraphs into chunks of approximately chunkSize characters
                var currentChunk = new StringBuilder();
                var currentChunkIndex = 0;

                foreach (var paragraph in paragraphs)
                {
                    // If adding this paragraph would exceed the chunk size, create a new chunk
                    if (currentChunk.Length > 0 && currentChunk.Length + paragraph.Length > chunkSize)
                    {
                        // Create a chunk from the current content
                        chunks.Add(CreateChunk(document, currentChunk.ToString(), currentChunkIndex));
                        
                        // Start a new chunk with overlap from the previous chunk
                        if (chunkOverlap > 0 && currentChunk.Length > chunkOverlap)
                        {
                            var overlapText = currentChunk.ToString().Substring(
                                Math.Max(0, currentChunk.Length - chunkOverlap));
                            currentChunk = new StringBuilder(overlapText);
                        }
                        else
                        {
                            currentChunk = new StringBuilder();
                        }
                        
                        currentChunkIndex++;
                    }

                    // Add the paragraph to the current chunk
                    if (currentChunk.Length > 0)
                    {
                        currentChunk.Append("\n\n");
                    }
                    currentChunk.Append(paragraph);
                }

                // Add the final chunk if it has content
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(document, currentChunk.ToString(), currentChunkIndex));
                }

                _logger.LogInformation("Created {ChunkCount} chunks for document: {DocumentTitle}", 
                    chunks.Count, document.Title);

                return chunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error chunking document with ID: {DocumentId}", document.Id);
                return new List<Chunk>();
            }
        }

        private Chunk CreateChunk(Document document, string content, int chunkIndex)
        {
            var chunk = new Chunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = document.Id,
                Content = content,
                ChunkIndex = chunkIndex,
                Metadata = new Dictionary<string, string>
                {
                    { "title", document.Title },
                    { "source", document.Source },
                    { "tags", string.Join(",", document.Tags) }  // Convert List<string> to comma-separated string
                }
                // Chunk doesn't have CreatedAt property
            };
            
            // Copy any additional metadata from the document
            if (document.Metadata != null)
            {
                foreach (var kvp in document.Metadata)
                {
                    if (!chunk.Metadata.ContainsKey(kvp.Key))
                    {
                        chunk.Metadata[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return chunk;
        }
    }
}