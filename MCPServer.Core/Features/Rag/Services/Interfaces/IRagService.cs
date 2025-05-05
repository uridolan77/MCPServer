using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;

namespace MCPServer.Core.Features.Rag.Services.Interfaces
{
    public interface IRagService
    {
        Task<Document> IndexDocumentAsync(Document document);
        Task<bool> DeleteDocumentAsync(string id);
        Task<SearchResult> SearchAsync(SearchRequest request);
        Task<string> GenerateAnswerWithContextAsync(string question, string sessionId);
    }
}

