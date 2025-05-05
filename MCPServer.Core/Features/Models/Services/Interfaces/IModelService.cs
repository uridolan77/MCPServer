using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Llm;

namespace MCPServer.Core.Features.Models.Services.Interfaces
{
    public interface IModelService
    {
        Task<List<LlmModel>> GetAvailableModelsAsync();
        Task<LlmModel?> GetModelByIdAsync(int modelId);
        Task<List<LlmModel>> GetEnabledModelsAsync();
        Task<LlmProvider?> GetProviderForModelAsync(LlmModel model);
    }
}
