using System.Collections.Generic;

namespace MCPServer.Core.Models.Rag
{
    public class EmbeddingRequest
    {
        public string Model { get; set; } = "text-embedding-ada-002";
        public List<string> Input { get; set; } = new List<string>();
    }
}
