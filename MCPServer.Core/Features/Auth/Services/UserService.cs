using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MCPServer.Core.Config;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MCPServer.Core.Features.Auth.Services.Interfaces;

namespace MCPServer.Core.Features.Auth.Services
{
    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        private readonly McpServerDbContext _dbContext;

        public UserService(
            IOptions<AppSettings> appSettings,
            McpServerDbContext dbContext)
        {
            _appSettings = appSettings.Value;
            _dbContext = dbContext;
        }

        public async Task<AuthResponse?> AuthenticateAsync(LoginRequest request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !VerifyPasswordHash(request.Password, user.PasswordHash))
            {
                return null;
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = GenerateRefreshToken(user.Id),
                ExpiresIn = _appSettings.Auth.AccessTokenExpirationMinutes * 60,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles
            };
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            // Check if username already exists
            if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
            {
                return null;
            }

            // Check if email already exists
            if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            {
                return null;
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Roles = new List<string> { "User" }, // Default role
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = GenerateRefreshToken(user.Id),
                ExpiresIn = _appSettings.Auth.AccessTokenExpirationMinutes * 60,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles
            };
        }

        public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
        {
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken && t.ExpiryDate > DateTime.UtcNow);

            if (token == null)
            {
                return null;
            }

            var user = await _dbContext.Users.FindAsync(token.UserId);
            if (user == null)
            {
                return null;
            }

            // Generate new tokens
            var newRefreshToken = GenerateRefreshToken(user.Id);

            // Revoke the old refresh token
            token.Revoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return new AuthResponse
            {
                AccessToken = GenerateJwtToken(user),
                RefreshToken = newRefreshToken,
                ExpiresIn = _appSettings.Auth.AccessTokenExpirationMinutes * 60,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles
            };
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (token == null)
            {
                return false;
            }

            token.Revoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _dbContext.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _dbContext.Users.ToListAsync();
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _dbContext.Users.Update(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteUserAsync(Guid id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            _dbContext.Users.Remove(user);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsSessionOwnedByUserAsync(string sessionId, string username)
        {
            var user = await GetUserByUsernameAsync(username);
            return user != null && user.OwnedSessionIds.Contains(sessionId);
        }

        public async Task AddSessionToUserAsync(string sessionId, string username)
        {
            var user = await GetUserByUsernameAsync(username);
            if (user != null && !user.OwnedSessionIds.Contains(sessionId))
            {
                user.OwnedSessionIds.Add(sessionId);
                await _dbContext.SaveChangesAsync();
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Auth.Secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Add roles as claims
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_appSettings.Auth.AccessTokenExpirationMinutes),
                Issuer = _appSettings.Auth.Issuer,
                Audience = _appSettings.Auth.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken(Guid userId)
        {
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rng.GetBytes(randomBytes);
            var refreshToken = Convert.ToBase64String(randomBytes);

            var token = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(_appSettings.Auth.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.RefreshTokens.Add(token);
            _dbContext.SaveChanges();

            return refreshToken;
        }

        private string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Combine salt and hash
            var hashBytes = new byte[salt.Length + hash.Length];
            Array.Copy(salt, 0, hashBytes, 0, salt.Length);
            Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);

            return Convert.ToBase64String(hashBytes);
        }

        private bool VerifyPasswordHash(string password, string storedHash)
        {
            var hashBytes = Convert.FromBase64String(storedHash);

            // Get salt (first 64 bytes)
            var salt = new byte[64];
            Array.Copy(hashBytes, 0, salt, 0, salt.Length);

            // Get stored hash (remaining bytes)
            var hash = new byte[hashBytes.Length - salt.Length];
            Array.Copy(hashBytes, salt.Length, hash, 0, hash.Length);

            // Compute hash with provided password and extracted salt
            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Compare computed hash with stored hash
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != hash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}




