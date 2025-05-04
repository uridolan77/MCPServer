using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MCPServer.Core.Models.Llm;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCPServer.Core.Data
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

        public async Task SeedAsync()
        {
            try
            {
                // Seed LLM providers if none exist
                if (!await _dbContext.LlmProviders.AnyAsync())
                {
                    _logger.LogInformation("Seeding LLM providers");

                    var providers = new List<LlmProvider>
                    {
                        new LlmProvider
                        {
                            Name = "OpenAI",
                            DisplayName = "OpenAI",
                            Description = "OpenAI API provider for GPT models",
                            ApiEndpoint = "https://api.openai.com/v1/chat/completions",
                            IsEnabled = true,
                            AuthType = "ApiKey",
                            CreatedAt = DateTime.UtcNow
                        },
                        new LlmProvider
                        {
                            Name = "Anthropic",
                            DisplayName = "Anthropic",
                            Description = "Anthropic API provider for Claude models",
                            ApiEndpoint = "https://api.anthropic.com/v1/messages",
                            IsEnabled = true,
                            AuthType = "ApiKey",
                            CreatedAt = DateTime.UtcNow
                        }
                    };

                    await _dbContext.LlmProviders.AddRangeAsync(providers);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("LLM providers seeded successfully");
                }

                // Seed LLM models if none exist
                if (!await _dbContext.LlmModels.AnyAsync())
                {
                    _logger.LogInformation("Seeding LLM models");

                    // Get provider IDs
                    var openAiProvider = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.Name == "OpenAI");
                    var anthropicProvider = await _dbContext.LlmProviders.FirstOrDefaultAsync(p => p.Name == "Anthropic");

                    if (openAiProvider != null)
                    {
                        var openAiModels = new List<LlmModel>
                        {
                            new LlmModel
                            {
                                ProviderId = openAiProvider.Id,
                                Name = "gpt-3.5-turbo",
                                Description = "GPT-3.5 Turbo model",
                                MaxTokens = 4096,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new LlmModel
                            {
                                ProviderId = openAiProvider.Id,
                                Name = "gpt-4",
                                Description = "GPT-4 model",
                                MaxTokens = 8192,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new LlmModel
                            {
                                ProviderId = openAiProvider.Id,
                                Name = "gpt-4-turbo",
                                Description = "GPT-4 Turbo model",
                                MaxTokens = 16384,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow
                            }
                        };

                        await _dbContext.LlmModels.AddRangeAsync(openAiModels);
                    }

                    if (anthropicProvider != null)
                    {
                        var anthropicModels = new List<LlmModel>
                        {
                            new LlmModel
                            {
                                ProviderId = anthropicProvider.Id,
                                Name = "claude-2",
                                Description = "Claude 2 model",
                                MaxTokens = 100000,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new LlmModel
                            {
                                ProviderId = anthropicProvider.Id,
                                Name = "claude-instant-1",
                                Description = "Claude Instant 1 model",
                                MaxTokens = 100000,
                                IsEnabled = true,
                                CreatedAt = DateTime.UtcNow
                            }
                        };

                        await _dbContext.LlmModels.AddRangeAsync(anthropicModels);
                    }

                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("LLM models seeded successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding LLM providers and models");
                throw;
            }
        }
    }
}
