using System.Collections.Generic;
using MCPServer.Core.Models;

namespace MCPServer.Core.Features.Auth.Services.Interfaces
{
    public interface ITokenManager
    {
        int CountTokens(string text);
        int CountMessageTokens(Message message);
        int CountContextTokens(SessionContext context);
        SessionContext TrimContextToFitTokenLimit(SessionContext context, int maxTokens);
        List<LlmMessage> ConvertToLlmMessages(List<Message> messages, string newUserInput);
    }
}
