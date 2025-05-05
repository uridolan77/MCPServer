using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MCPServer.Core.Models.Auth;

namespace MCPServer.Core.Features.Auth.Services.Interfaces
{
    public interface IUserService
    {
        Task<AuthResponse?> AuthenticateAsync(LoginRequest request);
        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
        Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> IsSessionOwnedByUserAsync(string sessionId, string username);
        Task AddSessionToUserAsync(string sessionId, string username);
    }
}

