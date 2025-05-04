using MCPServer.Core.Models.Auth;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MCPServer.Core.Data.DataSeeding
{
    public static class ModelBuilderExtensions
    {
        public static void SeedData(this ModelBuilder modelBuilder)
        {
            // Seed the admin user
            SeedAdminUser(modelBuilder);

            // Seed LLM providers and models
            SeedLlmProviders(modelBuilder);
        }

        private static void SeedAdminUser(ModelBuilder modelBuilder)
        {
            var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = adminId,
                    Username = "admin",
                    Email = "admin@mcpserver.com",
                    PasswordHash = CreatePasswordHash("Admin@123"),
                    Roles = new List<string> { "Admin", "User" },
                    IsActive = true,
                    CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastLoginAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    OwnedSessionIds = new List<string>()
                }
            );
        }

        private static void SeedLlmProviders(ModelBuilder modelBuilder)
        {
            // OpenAI Provider
            var openAiProviderId = 1;
            modelBuilder.Entity<LlmProvider>().HasData(
                new LlmProvider
                {
                    Id = openAiProviderId,
                    Name = "OpenAI",
                    ApiEndpoint = "https://api.openai.com/v1/chat/completions",
                    Description = "OpenAI's GPT models for text generation",
                    IsEnabled = true,
                    AuthType = "ApiKey",
                    ConfigSchema = @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""apiKey"": {
                                ""type"": ""string"",
                                ""description"": ""OpenAI API Key""
                            },
                            ""organization"": {
                                ""type"": ""string"",
                                ""description"": ""OpenAI Organization ID (optional)""
                            }
                        },
                        ""required"": [""apiKey""]
                    }"
                }
            );

            // OpenAI Models
            modelBuilder.Entity<LlmModel>().HasData(
                new LlmModel
                {
                    Id = 1,
                    ProviderId = openAiProviderId,
                    Name = "GPT-4o",
                    ModelId = "gpt-4o",
                    Description = "OpenAI's most capable model for text, vision, and audio tasks",
                    MaxTokens = 4096,
                    ContextWindow = 128000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    SupportsTools = true,
                    CostPer1KInputTokens = 0.005m,
                    CostPer1KOutputTokens = 0.015m,
                    IsEnabled = true
                },
                new LlmModel
                {
                    Id = 2,
                    ProviderId = openAiProviderId,
                    Name = "GPT-4 Turbo",
                    ModelId = "gpt-4-turbo",
                    Description = "OpenAI's most capable model optimized for speed",
                    MaxTokens = 4096,
                    ContextWindow = 128000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    SupportsTools = true,
                    CostPer1KInputTokens = 0.01m,
                    CostPer1KOutputTokens = 0.03m,
                    IsEnabled = true
                },
                new LlmModel
                {
                    Id = 3,
                    ProviderId = openAiProviderId,
                    Name = "GPT-3.5 Turbo",
                    ModelId = "gpt-3.5-turbo",
                    Description = "OpenAI's fastest and most cost-effective model",
                    MaxTokens = 4096,
                    ContextWindow = 16385,
                    SupportsStreaming = true,
                    SupportsVision = false,
                    SupportsTools = true,
                    CostPer1KInputTokens = 0.0005m,
                    CostPer1KOutputTokens = 0.0015m,
                    IsEnabled = true
                }
            );

            // Anthropic Provider
            var anthropicProviderId = 2;
            modelBuilder.Entity<LlmProvider>().HasData(
                new LlmProvider
                {
                    Id = anthropicProviderId,
                    Name = "Anthropic",
                    ApiEndpoint = "https://api.anthropic.com/v1/messages",
                    Description = "Anthropic's Claude models for text generation",
                    IsEnabled = true,
                    AuthType = "ApiKey",
                    ConfigSchema = @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""apiKey"": {
                                ""type"": ""string"",
                                ""description"": ""Anthropic API Key""
                            }
                        },
                        ""required"": [""apiKey""]
                    }"
                }
            );

            // Anthropic Models
            modelBuilder.Entity<LlmModel>().HasData(
                new LlmModel
                {
                    Id = 4,
                    ProviderId = anthropicProviderId,
                    Name = "Claude 3 Opus",
                    ModelId = "claude-3-opus-20240229",
                    Description = "Anthropic's most powerful model for highly complex tasks",
                    MaxTokens = 4096,
                    ContextWindow = 200000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    SupportsTools = true,
                    CostPer1KInputTokens = 0.015m,
                    CostPer1KOutputTokens = 0.075m,
                    IsEnabled = true
                },
                new LlmModel
                {
                    Id = 5,
                    ProviderId = anthropicProviderId,
                    Name = "Claude 3 Sonnet",
                    ModelId = "claude-3-sonnet-20240229",
                    Description = "Anthropic's balanced model for most tasks",
                    MaxTokens = 4096,
                    ContextWindow = 200000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    SupportsTools = true,
                    CostPer1KInputTokens = 0.003m,
                    CostPer1KOutputTokens = 0.015m,
                    IsEnabled = true
                },
                new LlmModel
                {
                    Id = 6,
                    ProviderId = anthropicProviderId,
                    Name = "Claude 3 Haiku",
                    ModelId = "claude-3-haiku-20240307",
                    Description = "Anthropic's fastest and most cost-effective model",
                    MaxTokens = 4096,
                    ContextWindow = 200000,
                    SupportsStreaming = true,
                    SupportsVision = true,
                    SupportsTools = true,
                    CostPer1KInputTokens = 0.00025m,
                    CostPer1KOutputTokens = 0.00125m,
                    IsEnabled = true
                }
            );
        }

        // Helper method for creating password hash with fixed salt for reproducible seeding
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

        // Helper methods for API key seeding (can be implemented in DatabaseInitializer for sensitive data)
        public static void SeedLlmProviderCredential(this McpServerDbContext context, string openAiApiKey)
        {
            // Find the OpenAI provider
            var openAiProvider = context.LlmProviders
                .FirstOrDefault(p => p.Name == "OpenAI");

            if (openAiProvider == null)
            {
                Console.WriteLine("OpenAI provider not found in the database.");
                return;
            }

            // Check if a credential already exists
            var existingCredential = context.LlmProviderCredentials
                .FirstOrDefault(c => c.ProviderId == openAiProvider.Id && c.UserId == null);

            if (existingCredential != null)
            {
                // Update existing credential
                existingCredential.EncryptedCredentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(
                        System.Text.Json.JsonSerializer.Serialize(new { ApiKey = openAiApiKey })
                    )
                );
                existingCredential.IsDefault = true;
                existingCredential.IsEnabled = true;
                existingCredential.UpdatedAt = DateTime.UtcNow;
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
                        Encoding.UTF8.GetBytes(
                            System.Text.Json.JsonSerializer.Serialize(new { ApiKey = openAiApiKey })
                        )
                    ),
                    IsDefault = true,
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.LlmProviderCredentials.Add(credential);
            }

            context.SaveChanges();
        }
    }
}