using System;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MCPServer.API.Features.Shared;
using MCPServer.API.Features.Rag.Models;

namespace MCPServer.API.Features.Rag.Controllers
{
    /// <summary>
    /// Controller for RAG (Retrieval-Augmented Generation) operations
    /// </summary>
    [ApiController]
    [Route("api/rag")]
    [Authorize]
    public class RagController : ApiControllerBase
    {
        private readonly IRagService _ragService;

        public RagController(
            ILogger<RagController> logger,
            IRagService ragService)
            : base(logger)
        {
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
        }

        /// <summary>
        /// Add a new document to the RAG system
        /// </summary>
        [HttpPost("documents")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<Document>>> AddDocument([FromBody] Document document)
        {
            try
            {
                var result = await _ragService.IndexDocumentAsync(document);
                return SuccessResponse(result, "Document added and indexed successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<Document>("Error adding document", ex);
            }
        }

        /// <summary>
        /// Delete a document from the RAG system
        /// </summary>
        [HttpDelete("documents/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteDocument(string id)
        {
            try
            {
                var result = await _ragService.DeleteDocumentAsync(id);

                if (!result)
                {
                    return NotFoundResponse<bool>($"Document with ID {id} not found");
                }

                return SuccessResponse(true, "Document deleted successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<bool>($"Error deleting document with ID {id}", ex);
            }
        }

        /// <summary>
        /// Search for documents in the RAG system
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<ApiResponse<SearchResult>>> Search([FromBody] SearchRequest request)
        {
            try
            {
                var result = await _ragService.SearchAsync(request);
                return SuccessResponse(result, "Search completed successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<SearchResult>($"Error searching for query: {request.Query}", ex);
            }
        }

        /// <summary>
        /// Generate an answer using the RAG system
        /// </summary>
        [HttpPost("answer")]
        public async Task<ActionResult<ApiResponse<AnswerResponse>>> GenerateAnswer([FromBody] AnswerRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Question))
                {
                    return BadRequestResponse<AnswerResponse>("Question is required");
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    return BadRequestResponse<AnswerResponse>("SessionId is required");
                }

                var answer = await _ragService.GenerateAnswerWithContextAsync(request.Question, request.SessionId);

                var response = new AnswerResponse
                {
                    Question = request.Question,
                    Answer = answer,
                    SessionId = request.SessionId
                };

                return SuccessResponse(response, "Answer generated successfully");
            }
            catch (Exception ex)
            {
                return ErrorResponse<AnswerResponse>($"Error generating answer for question: {request.Question}", ex);
            }
        }
    }
}


