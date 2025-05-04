using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Data.Repositories;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Rag
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDocumentRepository _documentRepository;
        private readonly ILogger<DocumentService> _logger;
        private readonly IEmbeddingService _embeddingService;

        public DocumentService(
            IUnitOfWork unitOfWork,
            IDocumentRepository documentRepository,
            IEmbeddingService embeddingService,
            ILogger<DocumentService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Document> AddDocumentAsync(Document document)
        {
            if (string.IsNullOrEmpty(document.Id))
            {
                document.Id = Guid.NewGuid().ToString();
            }

            document.CreatedAt = DateTime.UtcNow;
            await _documentRepository.AddAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Added document with ID: {DocumentId}", document.Id);
            return document;
        }

        public async Task<Document?> GetDocumentAsync(string id)
        {
            var document = await _documentRepository.GetByIdAsync(id);

            if (document == null)
            {
                _logger.LogWarning("Document not found with ID: {DocumentId}", id);
            }

            return document;
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            var documents = await _documentRepository.GetAllAsync();
            return documents.ToList();
        }

        public async Task<List<Document>> GetDocumentsByTagAsync(string tag)
        {
            var documents = await _documentRepository.GetDocumentsByTagAsync(tag);
            return documents.ToList();
        }

        public async Task<bool> UpdateDocumentAsync(Document document)
        {
            var existingDocument = await _documentRepository.GetByIdAsync(document.Id);

            if (existingDocument == null)
            {
                _logger.LogWarning("Cannot update document. Document not found with ID: {DocumentId}", document.Id);
                return false;
            }

            await _documentRepository.UpdateAsync(document);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated document with ID: {DocumentId}", document.Id);
            return true;
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            var document = await _documentRepository.GetByIdAsync(id);

            if (document == null)
            {
                _logger.LogWarning("Cannot delete document. Document not found with ID: {DocumentId}", id);
                return false;
            }

            await _documentRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

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

                // Combine paragraphs into chunks of approximately chunkSize characters
                var currentChunk = new StringBuilder();
                var chunkIndex = 0;

                foreach (var paragraph in paragraphs)
                {
                    // If adding this paragraph would exceed the chunk size, create a new chunk
                    if (currentChunk.Length > 0 && currentChunk.Length + paragraph.Length > chunkSize)
                    {
                        // Create a chunk from the current content
                        chunks.Add(CreateChunk(document.Id, currentChunk.ToString(), chunkIndex, document.Metadata));
                        chunkIndex++;

                        // Start a new chunk with overlap
                        var words = currentChunk.ToString().Split(' ');
                        var overlapWordCount = Math.Min(words.Length, chunkOverlap / 5); // Approximate words in overlap
                        
                        currentChunk.Clear();
                        if (overlapWordCount > 0)
                        {
                            currentChunk.Append(string.Join(" ", words.Skip(words.Length - overlapWordCount)));
                            currentChunk.Append(" ");
                        }
                    }

                    // Add the paragraph to the current chunk
                    currentChunk.Append(paragraph);
                    currentChunk.Append("\n\n");
                }

                // Add the final chunk if there's any content left
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(document.Id, currentChunk.ToString().Trim(), chunkIndex, document.Metadata));
                }

                _logger.LogInformation("Chunked document with ID: {DocumentId} into {ChunkCount} chunks",
                    document.Id, chunks.Count);

                return chunks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error chunking document with ID: {DocumentId}", document.Id);
                throw;
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

        public async Task<Dictionary<string, int>> GetTagStatisticsAsync()
        {
            return await _documentRepository.GetTagStatisticsAsync();
        }
    }
}
