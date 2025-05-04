using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Rag
{
    public class MySqlDocumentService : IDocumentService
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<MySqlDocumentService> _logger;

        public MySqlDocumentService(
            McpServerDbContext dbContext,
            ILogger<MySqlDocumentService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Document> AddDocumentAsync(Document document)
        {
            if (string.IsNullOrEmpty(document.Id))
            {
                document.Id = Guid.NewGuid().ToString();
            }

            document.CreatedAt = DateTime.UtcNow;
            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Added document with ID: {DocumentId}", document.Id);
            return document;
        }

        public async Task<Document?> GetDocumentAsync(string id)
        {
            var document = await _dbContext.Documents.FindAsync(id);

            if (document == null)
            {
                _logger.LogWarning("Document not found with ID: {DocumentId}", id);
            }

            return document;
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            return await _dbContext.Documents.ToListAsync();
        }

        public async Task<List<Document>> GetDocumentsByTagAsync(string tag)
        {
            // Since Tags is stored as JSON, we need to query differently
            // This is a simple approach that might not be efficient for large datasets
            var allDocuments = await _dbContext.Documents.ToListAsync();
            return allDocuments.Where(d => d.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        public async Task<bool> UpdateDocumentAsync(Document document)
        {
            var existingDocument = await _dbContext.Documents.FindAsync(document.Id);

            if (existingDocument == null)
            {
                _logger.LogWarning("Cannot update document. Document not found with ID: {DocumentId}", document.Id);
                return false;
            }

            _dbContext.Entry(existingDocument).CurrentValues.SetValues(document);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated document with ID: {DocumentId}", document.Id);
            return true;
        }

        public async Task<bool> DeleteDocumentAsync(string id)
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

        public async Task<List<Chunk>> ChunkDocumentAsync(Document document, int chunkSize = 1000, int chunkOverlap = 200)
        {
            try
            {
                var chunks = new List<Chunk>();
                var content = document.Content;

                // Simple chunking by paragraphs first
                var paragraphs = Regex.Split(content, @"\n\s*\n")
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .ToList();

                var currentChunk = new StringBuilder();
                var chunkIndex = 0;

                foreach (var paragraph in paragraphs)
                {
                    // If adding this paragraph would exceed the chunk size, create a new chunk
                    if (currentChunk.Length + paragraph.Length > chunkSize && currentChunk.Length > 0)
                    {
                        chunks.Add(CreateChunk(document.Id, currentChunk.ToString(), chunkIndex++, document.Metadata));

                        // Start a new chunk with overlap
                        var words = currentChunk.ToString().Split(' ');
                        currentChunk.Clear();

                        // Add overlap from previous chunk
                        if (words.Length > 0)
                        {
                            var overlapWordCount = Math.Min(words.Length, chunkOverlap / 5); // Approximate words for overlap
                            var overlapText = string.Join(" ", words.Skip(words.Length - overlapWordCount));
                            currentChunk.Append(overlapText).Append(" ");
                        }
                    }

                    currentChunk.Append(paragraph).Append("\n\n");
                }

                // Add the last chunk if there's any content left
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(document.Id, currentChunk.ToString().Trim(), chunkIndex, document.Metadata));
                }

                _logger.LogInformation("Created {ChunkCount} chunks for document {DocumentId}", chunks.Count, document.Id);
                return chunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error chunking document {DocumentId}", document.Id);
                return new List<Chunk>();
            }
        }

        private Chunk CreateChunk(string documentId, string content, int index, Dictionary<string, string> metadata)
        {
            var chunk = new Chunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = documentId,
                Content = content,
                ChunkIndex = index,
                Metadata = new Dictionary<string, string>(metadata)
            };

            // Add chunk index to metadata
            chunk.Metadata["chunk_index"] = index.ToString();

            return chunk;
        }

        // Add this method to support more efficient tag-based queries
        public async Task<IEnumerable<Document>> FindDocumentsByTagAsync(string tag)
        {
            try
            {
                // Using JSON_CONTAINS function in MySQL to query within the JSON array
                // This is more efficient than deserializing the entire Tags collection
                return await _dbContext.Documents
                    .FromSqlRaw(@"
                        SELECT * FROM Documents 
                        WHERE JSON_CONTAINS(Tags, JSON_QUOTE({0}))",
                        tag)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents by tag: {Tag}", tag);
                throw;
            }
        }

        // Add this method to get tag statistics
        public async Task<Dictionary<string, int>> GetTagStatisticsAsync()
        {
            var results = new Dictionary<string, int>();
            
            try
            {
                // Load all documents (not efficient for large datasets, but shows the concept)
                var documents = await _dbContext.Documents.ToListAsync();
                
                // Extract and count tags
                foreach (var document in documents)
                {
                    foreach (var tag in document.Tags)
                    {
                        if (results.ContainsKey(tag))
                            results[tag]++;
                        else
                            results[tag] = 1;
                    }
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag statistics");
                throw;
            }
        }
    }
}
