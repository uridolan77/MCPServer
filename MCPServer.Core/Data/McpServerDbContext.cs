using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MCPServer.Core.Models;
using MCPServer.Core.Models.Auth;
using MCPServer.Core.Models.Llm;
using MCPServer.Core.Models.Rag;
using MCPServer.Core.Data.DataSeeding;

namespace MCPServer.Core.Data
{
    public class McpServerDbContext : DbContext
    {
        public McpServerDbContext(DbContextOptions<McpServerDbContext> options)
            : base(options)
        {
        }

        // Auth tables
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        // RAG tables
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Chunk> Chunks { get; set; } = null!;

        // Session tables
        public DbSet<SessionData> Sessions { get; set; } = null!;

        // Monitoring tables
        public DbSet<ApiLog> ApiLogs { get; set; } = null!;
        public DbSet<ErrorLog> ErrorLogs { get; set; } = null!;
        public DbSet<UsageMetric> UsageMetrics { get; set; } = null!;
        public DbSet<ChatUsageLog> ChatUsageLogs { get; set; } = null!;

        // LLM tables
        public DbSet<LlmProvider> LlmProviders { get; set; } = null!;
        public DbSet<LlmModel> LlmModels { get; set; } = null!;
        public DbSet<LlmProviderCredential> LlmProviderCredentials { get; set; } = null!;
        public DbSet<LlmUsageLog> LlmUsageLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();

                // Store Roles as JSON array
                entity.Property(e => e.Roles).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );

                // Store OwnedSessionIds as JSON array
                entity.Property(e => e.OwnedSessionIds).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );
            });

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ExpiryDate).IsRequired();
            });

            // Configure Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Content).IsRequired().HasColumnType("LONGTEXT");

                // Store Tags as JSON array
                entity.Property(e => e.Tags).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                );

                // Add indexes for faster lookups
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Source);

                // Store Metadata as JSON
                entity.Property(e => e.Metadata).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                );
            });

            // Configure Chunk entity
            modelBuilder.Entity<Chunk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DocumentId).IsRequired();
                entity.Property(e => e.Content).IsRequired().HasColumnType("LONGTEXT");

                // Store Embedding as JSON array
                entity.Property(e => e.Embedding).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<float>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<float>()
                );

                // Add indexes for faster lookups
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.ChunkIndex);

                // Store Metadata as JSON
                entity.Property(e => e.Metadata).HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                );

                // Create index on DocumentId for faster lookups
                entity.HasIndex(e => e.DocumentId);
            });

            // Configure SessionData entity
            modelBuilder.Entity<SessionData>(entity =>
            {
                entity.HasKey(e => e.SessionId);
                entity.Property(e => e.UserId).IsRequired(false); // Can be null for anonymous sessions
                entity.Property(e => e.Data).IsRequired().HasColumnType("LONGTEXT");
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.LastUpdatedAt).IsRequired();

                // Create index on UserId for faster lookups
                entity.HasIndex(e => e.UserId);
            });

            // Configure ApiLog entity
            modelBuilder.Entity<ApiLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Endpoint).IsRequired();
                entity.Property(e => e.Method).IsRequired();
                entity.Property(e => e.UserId).IsRequired(false); // Can be null for anonymous requests
                entity.Property(e => e.RequestBody).HasColumnType("LONGTEXT");
                entity.Property(e => e.ResponseBody).HasColumnType("LONGTEXT");
                entity.Property(e => e.Timestamp).IsRequired();

                // Create index on Timestamp for faster lookups
                entity.HasIndex(e => e.Timestamp);
                // Create index on UserId for faster lookups
                entity.HasIndex(e => e.UserId);
            });

            // Configure ErrorLog entity
            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.StackTrace).HasColumnType("LONGTEXT");
                entity.Property(e => e.UserId).IsRequired(false); // Can be null for anonymous requests
                entity.Property(e => e.Timestamp).IsRequired();

                // Create index on Timestamp for faster lookups
                entity.HasIndex(e => e.Timestamp);
            });

            // Configure UsageMetric entity
            modelBuilder.Entity<UsageMetric>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired(false); // Can be null for anonymous requests
                entity.Property(e => e.MetricType).IsRequired();
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();

                // Create index on Timestamp for faster lookups
                entity.HasIndex(e => e.Timestamp);
                // Create index on UserId for faster lookups
                entity.HasIndex(e => e.UserId);
                // Create index on MetricType for faster lookups
                entity.HasIndex(e => e.MetricType);
            });

            // Configure LlmProvider entity
            modelBuilder.Entity<LlmProvider>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ApiEndpoint).IsRequired();
                entity.Property(e => e.Description).HasColumnType("TEXT");
                entity.Property(e => e.ConfigSchema).HasColumnType("TEXT");

                // Create unique index on Name
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure LlmModel entity
            modelBuilder.Entity<LlmModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ModelId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasColumnType("TEXT");

                // Create relationship with LlmProvider
                entity.HasOne(e => e.Provider)
                    .WithMany(p => p.Models)
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create unique index on ProviderId and ModelId
                entity.HasIndex(e => new { e.ProviderId, e.ModelId }).IsUnique();
            });

            // Configure LlmProviderCredential entity
            modelBuilder.Entity<LlmProviderCredential>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EncryptedCredentials).IsRequired().HasColumnType("TEXT");

                // Create relationship with LlmProvider
                entity.HasOne(e => e.Provider)
                    .WithMany(p => p.Credentials)
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Create index on UserId for faster lookups
                entity.HasIndex(e => e.UserId);

                // Create unique index on ProviderId, UserId, and Name
                entity.HasIndex(e => new { e.ProviderId, e.UserId, e.Name }).IsUnique();
            });

            // Configure LlmUsageLog entity
            modelBuilder.Entity<LlmUsageLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ErrorMessage).HasColumnType("TEXT");

                // Create relationship with LlmModel
                entity.HasOne(e => e.Model)
                    .WithMany()
                    .HasForeignKey(e => e.ModelId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Create relationship with LlmProviderCredential
                entity.HasOne(e => e.Credential)
                    .WithMany()
                    .HasForeignKey(e => e.CredentialId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Create index on UserId for faster lookups
                entity.HasIndex(e => e.UserId);

                // Create index on Timestamp for faster lookups
                entity.HasIndex(e => e.Timestamp);

                // Create index on ModelId for faster lookups
                entity.HasIndex(e => e.ModelId);

                // Create index on IsSuccessful for faster lookups
                entity.HasIndex(e => e.IsSuccessful);
            });

            // Configure ChatUsageLog entity
            modelBuilder.Entity<ChatUsageLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Message).HasColumnType("TEXT");
                entity.Property(e => e.Response).HasColumnType("TEXT");
                entity.Property(e => e.ErrorMessage).HasColumnType("TEXT");

                // Create index on ModelId for faster lookups
                entity.HasIndex(e => e.ModelId);

                // Create index on ProviderId for faster lookups
                entity.HasIndex(e => e.ProviderId);

                // Create index on UserId for faster lookups
                entity.HasIndex(e => e.UserId);

                // Create index on Timestamp for faster lookups
                entity.HasIndex(e => e.Timestamp);

                // Create index on SessionId for faster lookups
                entity.HasIndex(e => e.SessionId);
            });

            // Seed data using the consolidated approach
            modelBuilder.SeedData();
        }

        public void SeedLlmProviderCredential(string apiKey)
        {
            // Check if there are any credentials
            if (!LlmProviderCredentials.Any())
            {
                // Get the OpenAI provider
                var openAiProvider = LlmProviders.FirstOrDefault(p => p.Name == "OpenAI");

                if (openAiProvider != null)
                {
                    // Create a credential
                    var credential = new LlmProviderCredential
                    {
                        ProviderId = openAiProvider.Id,
                        Name = "Default OpenAI API Key",
                        IsEnabled = true,
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Set the API key
                    credential.SetCredentials(new { api_key = apiKey }, "your-encryption-key");

                    // Add the credential
                    LlmProviderCredentials.Add(credential);
                    SaveChanges();
                }
            }
        }
    }
}
