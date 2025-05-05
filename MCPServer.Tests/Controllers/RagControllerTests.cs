using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.API.Features.Rag.Controllers;
using MCPServer.API.Features.Rag.Models;
using MCPServer.Core.Features.Rag.Services.Interfaces;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Rag;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MCPServer.Tests.Controllers
{
    public class RagControllerTests
    {
        private readonly Mock<IRagService> _mockRagService;
        private readonly Mock<ILogger<RagController>> _mockLogger;
        private readonly RagController _controller;

        public RagControllerTests()
        {
            _mockRagService = new Mock<IRagService>();
            _mockLogger = new Mock<ILogger<RagController>>();
            _controller = new RagController(_mockRagService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task IndexDocument_WithValidDocument_ReturnsSuccessResponse()
        {
            // Arrange
            var document = new Document
            {
                Id = "test-doc-1",
                Title = "Test Document",
                Content = "This is a test document content.",
                Metadata = new Dictionary<string, string>
                {
                    { "author", "Test Author" },
                    { "category", "Test" }
                }
            };

            _mockRagService.Setup(x => x.IndexDocumentAsync(It.IsAny<Document>()))
                .ReturnsAsync(document);

            // Act
            var result = await _controller.IndexDocument(document);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<Document>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<Document>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Document indexed successfully", apiResponse.Message);
            Assert.Equal(document, apiResponse.Data);
        }

        [Fact]
        public async Task IndexDocument_WithNullDocument_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.IndexDocument(null);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<Document>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<Document>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Document cannot be null", apiResponse.Message);
        }

        [Fact]
        public async Task IndexDocument_WhenExceptionOccurs_ReturnsErrorResponse()
        {
            // Arrange
            var document = new Document
            {
                Id = "test-doc-1",
                Title = "Test Document",
                Content = "This is a test document content."
            };

            _mockRagService.Setup(x => x.IndexDocumentAsync(It.IsAny<Document>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.IndexDocument(document);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<Document>>>(result);
            var statusCodeResult = Assert.IsType<ObjectResult>(actionResult.Result);
            
            Assert.Equal(500, statusCodeResult.StatusCode);
            
            var apiResponse = Assert.IsType<ApiResponse<Document>>(statusCodeResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Equal("Error indexing document", apiResponse.Message);
        }

        [Fact]
        public async Task DeleteDocument_WithValidId_ReturnsSuccessResponse()
        {
            // Arrange
            var documentId = "test-doc-1";

            _mockRagService.Setup(x => x.DeleteDocumentAsync(documentId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteDocument(documentId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Document deleted successfully", apiResponse.Message);
            Assert.True(apiResponse.Data);
        }

        [Fact]
        public async Task DeleteDocument_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var documentId = "non-existent-doc";

            _mockRagService.Setup(x => x.DeleteDocumentAsync(documentId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteDocument(documentId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<bool>>>(result);
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<bool>>(notFoundResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Document not found", apiResponse.Message);
            Assert.False(apiResponse.Data);
        }

        [Fact]
        public async Task GenerateAnswer_WithValidRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var request = new AnswerRequest
            {
                Question = "What is RAG?",
                SessionId = "test-session"
            };

            var expectedAnswer = "RAG stands for Retrieval-Augmented Generation, a technique that combines retrieval of relevant documents with text generation.";

            _mockRagService.Setup(x => x.GenerateAnswerWithContextAsync(request.Question, request.SessionId))
                .ReturnsAsync(expectedAnswer);

            // Act
            var result = await _controller.GenerateAnswer(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<AnswerResponse>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<AnswerResponse>>(okResult.Value);
            
            Assert.True(apiResponse.Success);
            Assert.Equal("Answer generated successfully", apiResponse.Message);
            Assert.Equal(expectedAnswer, apiResponse.Data.Answer);
        }

        [Fact]
        public async Task GenerateAnswer_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GenerateAnswer(null);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<AnswerResponse>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<AnswerResponse>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Request cannot be null", apiResponse.Message);
        }

        [Fact]
        public async Task GenerateAnswer_WithEmptyQuestion_ReturnsBadRequest()
        {
            // Arrange
            var request = new AnswerRequest
            {
                Question = "",
                SessionId = "test-session"
            };

            // Act
            var result = await _controller.GenerateAnswer(request);

            // Assert
            var actionResult = Assert.IsType<ActionResult<ApiResponse<AnswerResponse>>>(result);
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<AnswerResponse>>(badRequestResult.Value);
            
            Assert.False(apiResponse.Success);
            Assert.Equal("Question cannot be empty", apiResponse.Message);
        }
    }
}
