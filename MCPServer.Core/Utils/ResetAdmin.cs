using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Utils
{
    public class ResetAdmin
    {
        public static async Task ResetAdminUser(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<McpServerDbContext>();

            // Check if admin user exists
            var adminUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == "admin");

            // Create a fixed password hash that will work with the verification logic
            string passwordHash = CreatePasswordHash("Admin@123");

            if (adminUser != null)
            {
                // Update existing admin user
                adminUser.PasswordHash = passwordHash;
                adminUser.IsActive = true;
                adminUser.LastLoginAt = DateTime.UtcNow;
            }
            else
            {
                // Create new admin user
                adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Email = "admin@mcpserver.com",
                    PasswordHash = passwordHash,
                    Roles = new List<string> { "Admin", "User" },
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    OwnedSessionIds = new List<string>()
                };
                dbContext.Users.Add(adminUser);
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine("Admin user reset successfully");
        }

        private static string CreatePasswordHash(string password)
        {
            // Create a fixed salt for reproducibility
            byte[] salt = new byte[64];
            for (int i = 0; i < salt.Length; i++)
            {
                salt[i] = (byte)i;
            }

            // Create hash with fixed salt
            using var hmac = new HMACSHA512(salt);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Combine salt and hash
            var hashBytes = new byte[salt.Length + hash.Length];
            Array.Copy(salt, 0, hashBytes, 0, salt.Length);
            Array.Copy(hash, 0, hashBytes, salt.Length, hash.Length);

            return Convert.ToBase64String(hashBytes);
        }
    }
}
