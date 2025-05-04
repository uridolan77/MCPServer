using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;

namespace MCPServer.Core.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<Document> AddDocumentAsync(Document document);
        Task<Document?> GetDocumentAsync(string id);
        Task<List<Document>> GetAllDocumentsAsync();
        Task<List<Document>> GetDocumentsByTagAsync(string tag);
        Task<bool> UpdateDocumentAsync(Document document);
        Task<bool> DeleteDocumentAsync(string id);
        Task<List<Chunk>> ChunkDocumentAsync(Document document, int chunkSize = 1000, int chunkOverlap = 200);
    }
}
