using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MCPServer.Core.Models.Rag
{
    public class EmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = new List<EmbeddingData>();

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public EmbeddingUsage Usage { get; set; } = new EmbeddingUsage();
    }

    public class EmbeddingData
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("embedding")]
        public List<float> Embedding { get; set; } = new List<float>();

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class EmbeddingUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
