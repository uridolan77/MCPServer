using System;
using System.Collections.Generic;

namespace MCPServer.Core.Models.Rag
{
    public class Chunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DocumentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public List<float> Embedding { get; set; } = new List<float>();
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
