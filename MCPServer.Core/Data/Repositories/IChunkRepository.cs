using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository interface for Chunk entities
    /// </summary>
    public interface IChunkRepository : IRepository<Chunk>
    {
        /// <summary>
        /// Gets chunks by document ID
        /// </summary>
        Task<IEnumerable<Chunk>> GetChunksByDocumentIdAsync(string documentId);
        
        /// <summary>
        /// Deletes chunks by document ID
        /// </summary>
        Task<bool> DeleteChunksByDocumentIdAsync(string documentId);
        
        /// <summary>
        /// Gets chunks by metadata
        /// </summary>
        Task<IEnumerable<Chunk>> GetChunksByMetadataAsync(Dictionary<string, string> metadata);
    }
}

