using System;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MCPServer.Utils
{
    public class AddOpenAiKey
    {
        public static async Task AddOpenAiKeyToDatabase(IServiceProvider serviceProvider, string apiKey)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<McpServerDbContext>();

            // Find the OpenAI provider
            var openAiProvider = await dbContext.LlmProviders
                .FirstOrDefaultAsync(p => p.Name == "OpenAI");

            if (openAiProvider == null)
            {
                Console.WriteLine("OpenAI provider not found in the database.");
                return;
            }

            // Check if a credential already exists
            var existingCredential = await dbContext.LlmProviderCredentials
                .FirstOrDefaultAsync(c => c.ProviderId == openAiProvider.Id && c.UserId == null);

            if (existingCredential != null)
            {
                // Update existing credential
                existingCredential.EncryptedCredentials = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        System.Text.Json.JsonSerializer.Serialize(new { ApiKey = apiKey })
                    )
                );
                existingCredential.IsDefault = true;
                existingCredential.IsEnabled = true;
                existingCredential.UpdatedAt = DateTime.UtcNow;

                dbContext.LlmProviderCredentials.Update(existingCredential);
                await dbContext.SaveChangesAsync();

                Console.WriteLine($"Updated OpenAI API key for credential ID: {existingCredential.Id}");
            }
            else
            {
                // Create new credential
                var credential = new LlmProviderCredential
                {
                    ProviderId = openAiProvider.Id,
                    UserId = null, // System-wide credential
                    Name = "Default OpenAI API Key",
                    EncryptedCredentials = Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes(
                            System.Text.Json.JsonSerializer.Serialize(new { ApiKey = apiKey })
                        )
                    ),
                    IsDefault = true,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.LlmProviderCredentials.Add(credential);
                await dbContext.SaveChangesAsync();

                Console.WriteLine($"Added OpenAI API key as credential ID: {credential.Id}");
            }
        }
    }
}
