using System.Collections.Generic;

namespace MCPServer.Core.Models.Rag
{
    public class SearchResult
    {
        public List<ChunkSearchResult> Results { get; set; } = new List<ChunkSearchResult>();
    }

    public class ChunkSearchResult
    {
        public Chunk Chunk { get; set; } = new Chunk();
        public float Score { get; set; }
        public Document Document { get; set; } = new Document();
    }
}
