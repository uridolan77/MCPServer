using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;

namespace MCPServer.Core.Services.Interfaces
{
    public interface ILlmService
    {
        Task<string> SendWithContextAsync(string userInput, SessionContext context, string? specificModelId = null);
        Task<LlmResponse> SendRequestAsync(LlmRequest request);
        Task StreamResponseAsync(LlmRequest request, Func<string, bool, Task> onChunkReceived);
    }
}
