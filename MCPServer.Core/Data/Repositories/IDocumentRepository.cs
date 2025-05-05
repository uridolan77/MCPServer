using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Features.Shared.Services.Interfaces;

namespace MCPServer.Core.Data.Repositories
{
    /// <summary>
    /// Repository interface for Document entities
    /// </summary>
    public interface IDocumentRepository : IRepository<Document>
    {
        /// <summary>
        /// Gets documents by tag
        /// </summary>
        Task<IEnumerable<Document>> GetDocumentsByTagAsync(string tag);
        
        /// <summary>
        /// Gets documents by multiple tags (AND logic)
        /// </summary>
        Task<IEnumerable<Document>> GetDocumentsByTagsAsync(IEnumerable<string> tags);
        
        /// <summary>
        /// Gets tag statistics (tag name and count)
        /// </summary>
        Task<Dictionary<string, int>> GetTagStatisticsAsync();
    }
}

