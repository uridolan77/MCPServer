using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCPServer.Core.Services.Interfaces
{
    public interface IEmbeddingService
    {
        Task<List<float>> GetEmbeddingAsync(string text);
        Task<List<List<float>>> GetEmbeddingsAsync(List<string> texts);
        float CalculateCosineSimilarity(List<float> embedding1, List<float> embedding2);
    }
}
