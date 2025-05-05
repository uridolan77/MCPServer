using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;

namespace MCPServer.Core.Features.Rag.Services.Interfaces
{
    public interface IVectorDbService
    {
        Task<bool> AddChunkAsync(Chunk chunk);
        Task<bool> AddChunksAsync(List<Chunk> chunks);
        Task<Chunk?> GetChunkAsync(string id);
        Task<List<Chunk>> GetChunksByDocumentIdAsync(string documentId);
        Task<bool> UpdateChunkAsync(Chunk chunk);
        Task<bool> DeleteChunkAsync(string id);
        Task<bool> DeleteChunksByDocumentIdAsync(string documentId);
        Task<List<ChunkSearchResult>> SearchAsync(string query, int topK = 3, float minScore = 0.7f, Dictionary<string, string>? metadata = null);
        Task<List<ChunkSearchResult>> SearchByEmbeddingAsync(List<float> embedding, int topK = 3, float minScore = 0.7f, Dictionary<string, string>? metadata = null);
    }
}
