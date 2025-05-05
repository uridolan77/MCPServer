using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models;

namespace MCPServer.Core.Features.Sessions.Services.Interfaces
{
    /// <summary>
    /// Service for managing session contexts
    /// </summary>
    public interface ISessionContextService
    {
        /// <summary>
        /// Gets or creates a session context
        /// </summary>
        Task<SessionContext> GetOrCreateSessionContextAsync(string sessionId);
        
        /// <summary>
        /// Adds a user message to the session context
        /// </summary>
        Task AddUserMessageAsync(string sessionId, string message);
        
        /// <summary>
        /// Adds an assistant message to the session context
        /// </summary>
        Task AddAssistantMessageAsync(string sessionId, string message);
    }
}

