using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services.Rag
{
    public class RagService : IRagService
    {
        private readonly ILogger<RagService> _logger;
        private readonly IDocumentService _documentService;
        private readonly IVectorDbService _vectorDbService;
        private readonly ILlmService _llmService;
        private readonly IContextService _contextService;

        public RagService(
            ILogger<RagService> logger,
            IDocumentService documentService,
            IVectorDbService vectorDbService,
            ILlmService llmService,
            IContextService contextService)
        {
            _logger = logger;
            _documentService = documentService;
            _vectorDbService = vectorDbService;
            _llmService = llmService;
            _contextService = contextService;
        }

        public async Task<Document> IndexDocumentAsync(Document document)
        {
            try
            {
                // Add document to document store
                var savedDocument = await _documentService.AddDocumentAsync(document);

                // Chunk the document
                var chunks = await _documentService.ChunkDocumentAsync(savedDocument);

                // Add chunks to vector database
                await _vectorDbService.AddChunksAsync(chunks);

                _logger.LogInformation("Indexed document with ID: {DocumentId} into {ChunkCount} chunks",
                    savedDocument.Id, chunks.Count);

                return savedDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document with title: {DocumentTitle}", document.Title);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            try
            {
                // Delete chunks from vector database
                await _vectorDbService.DeleteChunksByDocumentIdAsync(id);

                // Delete document from document store
                var result = await _documentService.DeleteDocumentAsync(id);

                _logger.LogInformation("Deleted document with ID: {DocumentId}", id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID: {DocumentId}", id);
                return false;
            }
        }

        public async Task<SearchResult> SearchAsync(SearchRequest request)
        {
            try
            {
                // Search for relevant chunks
                var metadata = request.Metadata;
                var chunkResults = await _vectorDbService.SearchAsync(
                    request.Query,
                    request.TopK,
                    request.MinScore,
                    metadata);

                var result = new SearchResult
                {
                    Results = chunkResults
                };

                _logger.LogInformation("Search for '{Query}' returned {ResultCount} results",
                    request.Query, chunkResults.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for query: {Query}", request.Query);
                return new SearchResult();
            }
        }

        public async Task<string> GenerateAnswerWithContextAsync(string question, string sessionId)
        {
            try
            {
                // Get session context
                var context = await _contextService.GetSessionContextAsync(sessionId);

                // Search for relevant chunks
                var searchResults = await _vectorDbService.SearchAsync(question, 3, 0.7f);

                if (searchResults.Count == 0)
                {
                    _logger.LogInformation("No relevant context found for question: {Question}", question);
                    return await _llmService.SendWithContextAsync(question, context);
                }

                // Build context from search results
                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("Here is some relevant information to help answer the question:");
                contextBuilder.AppendLine();

                foreach (var result in searchResults)
                {
                    contextBuilder.AppendLine($"Source: {result.Document.Title}");
                    contextBuilder.AppendLine($"Content: {result.Chunk.Content}");
                    contextBuilder.AppendLine();
                }

                contextBuilder.AppendLine($"Question: {question}");
                contextBuilder.AppendLine();
                contextBuilder.AppendLine("Please answer the question based on the provided information. If the information doesn't contain the answer, say so.");

                var enhancedPrompt = contextBuilder.ToString();

                // Add a system message to the context
                if (!context.Messages.Any(m => m.Role == "system"))
                {
                    context.Messages.Add(new Core.Models.Message
                    {
                        Role = "system",
                        Content = "You are a helpful assistant that answers questions based on the provided context. " +
                                 "Always cite your sources when using information from the context.",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Send the enhanced prompt to the LLM
                return await _llmService.SendWithContextAsync(enhancedPrompt, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating answer with context for question: {Question}", question);
                return "I'm sorry, but I encountered an error while trying to answer your question.";
            }
        }
    }
}
