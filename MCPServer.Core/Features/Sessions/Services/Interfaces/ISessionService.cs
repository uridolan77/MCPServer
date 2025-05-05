using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;

namespace MCPServer.Core.Features.Sessions.Services.Interfaces
{
    public interface ISessionService
    {
        Task<List<Message>> GetSessionHistoryAsync(string sessionId);
        Task SaveSessionDataAsync(string sessionId, List<Message> sessionHistory);
        Task<bool> DeleteSessionAsync(string sessionId);
        Task<List<SessionData>> GetAllSessionsAsync(int page, int pageSize);
        Task<SessionData> GetSessionDataAsync(string sessionId);
    }
}
