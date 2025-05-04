using System.Collections.Generic;

namespace MCPServer.Core.Models.Rag
{
    public class SearchRequest
    {
        public string Query { get; set; } = string.Empty;
        public int TopK { get; set; } = 3;
        public float MinScore { get; set; } = 0.7f;
        public List<string>? Tags { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
