using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Data;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Services
{
    public class LlmProviderSeeder
    {
        private readonly McpServerDbContext _dbContext;
        private readonly ILogger<LlmProviderSeeder> _logger;

        public LlmProviderSeeder(
            McpServerDbContext dbContext,
            ILogger<LlmProviderSeeder> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SeedProvidersAndModelsAsync()
        {
            try
            {
                // Check if providers already exist
                if (await _dbContext.LlmProviders.AnyAsync())
                {
                    _logger.LogInformation("LLM providers already exist, skipping seeding");
                    return;
                }

                _logger.LogInformation("Seeding LLM providers and models...");

                // Add OpenAI provider
                var openAiProvider = new LlmProvider
                {
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
                };

                _dbContext.LlmProviders.Add(openAiProvider);
                await _dbContext.SaveChangesAsync();

                // Add OpenAI models
                var openAiModels = new List<LlmModel>
                {
                    new LlmModel
                    {
                        ProviderId = openAiProvider.Id,
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
                        ProviderId = openAiProvider.Id,
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
                        ProviderId = openAiProvider.Id,
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
                };

                _dbContext.LlmModels.AddRange(openAiModels);

                // Add Anthropic provider
                var anthropicProvider = new LlmProvider
                {
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
                };

                _dbContext.LlmProviders.Add(anthropicProvider);
                await _dbContext.SaveChangesAsync();

                // Add Anthropic models
                var anthropicModels = new List<LlmModel>
                {
                    new LlmModel
                    {
                        ProviderId = anthropicProvider.Id,
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
                        ProviderId = anthropicProvider.Id,
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
                        ProviderId = anthropicProvider.Id,
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
                };

                _dbContext.LlmModels.AddRange(anthropicModels);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded LLM providers and models");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding LLM providers and models");
            }
        }
    }
}
