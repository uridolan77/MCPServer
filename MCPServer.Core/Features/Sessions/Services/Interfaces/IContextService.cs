using System.Threading.Tasks;
using MCPServer.Core.Models;

namespace MCPServer.Core.Features.Sessions.Services.Interfaces
{
    public interface IContextService
    {
        Task<SessionContext> GetSessionContextAsync(string sessionId);
        Task SaveContextAsync(SessionContext context);
        Task UpdateContextAsync(string sessionId, string userInput, string assistantResponse);
        Task<bool> DeleteSessionAsync(string sessionId);
    }
}

