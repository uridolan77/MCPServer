using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MCPServer.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddChatUsageLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ApiLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Endpoint = table.Column<string>(type: "longtext", nullable: false),
                    Method = table.Column<string>(type: "longtext", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    RequestBody = table.Column<string>(type: "LONGTEXT", nullable: true),
                    ResponseBody = table.Column<string>(type: "LONGTEXT", nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IpAddress = table.Column<string>(type: "longtext", nullable: true),
                    UserAgent = table.Column<string>(type: "longtext", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ChatUsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    ModelName = table.Column<string>(type: "longtext", nullable: true),
                    ProviderId = table.Column<int>(type: "int", nullable: true),
                    ProviderName = table.Column<string>(type: "longtext", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Response = table.Column<string>(type: "TEXT", nullable: false),
                    InputTokenCount = table.Column<int>(type: "int", nullable: false),
                    OutputTokenCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Success = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatUsageLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Chunks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    DocumentId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Content = table.Column<string>(type: "LONGTEXT", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    Embedding = table.Column<string>(type: "longtext", nullable: false),
                    Metadata = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chunks", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false),
                    Content = table.Column<string>(type: "LONGTEXT", nullable: false),
                    Source = table.Column<string>(type: "longtext", nullable: false),
                    Url = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Tags = table.Column<string>(type: "longtext", nullable: false),
                    Metadata = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Message = table.Column<string>(type: "longtext", nullable: false),
                    StackTrace = table.Column<string>(type: "LONGTEXT", nullable: true),
                    Source = table.Column<string>(type: "longtext", nullable: true),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    RequestPath = table.Column<string>(type: "longtext", nullable: true),
                    RequestMethod = table.Column<string>(type: "longtext", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LlmProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "longtext", nullable: false),
                    ApiEndpoint = table.Column<string>(type: "longtext", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AuthType = table.Column<string>(type: "longtext", nullable: false),
                    ConfigSchema = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmProviders", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Token = table.Column<string>(type: "longtext", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Revoked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "varchar(255)", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Data = table.Column<string>(type: "LONGTEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.SessionId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UsageMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    MetricType = table.Column<string>(type: "varchar(255)", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<string>(type: "longtext", nullable: true),
                    AdditionalData = table.Column<string>(type: "longtext", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageMetrics", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Username = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "longtext", nullable: false),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false),
                    FirstName = table.Column<string>(type: "longtext", nullable: true),
                    LastName = table.Column<string>(type: "longtext", nullable: true),
                    Roles = table.Column<string>(type: "longtext", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OwnedSessionIds = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LlmModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProviderId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    ContextWindow = table.Column<int>(type: "int", nullable: false),
                    SupportsStreaming = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SupportsVision = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SupportsTools = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CostPer1KInputTokens = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostPer1KOutputTokens = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LlmModels_LlmProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "LlmProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LlmProviderCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ProviderId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    ApiKey = table.Column<string>(type: "longtext", nullable: false),
                    EncryptedCredentials = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmProviderCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LlmProviderCredentials_LlmProviders_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "LlmProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LlmUsageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    CredentialId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsStreaming = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmUsageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LlmUsageLogs_LlmModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "LlmModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LlmUsageLogs_LlmProviderCredentials_CredentialId",
                        column: x => x.CredentialId,
                        principalTable: "LlmProviderCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "LlmProviders",
                columns: new[] { "Id", "ApiEndpoint", "AuthType", "ConfigSchema", "CreatedAt", "Description", "DisplayName", "IsEnabled", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "https://api.openai.com/v1/chat/completions", "ApiKey", "{\r\n                        \"type\": \"object\",\r\n                        \"properties\": {\r\n                            \"apiKey\": {\r\n                                \"type\": \"string\",\r\n                                \"description\": \"OpenAI API Key\"\r\n                            },\r\n                            \"organization\": {\r\n                                \"type\": \"string\",\r\n                                \"description\": \"OpenAI Organization ID (optional)\"\r\n                            }\r\n                        },\r\n                        \"required\": [\"apiKey\"]\r\n                    }", new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(615), "OpenAI's GPT models for text generation", "", true, "OpenAI", null },
                    { 2, "https://api.anthropic.com/v1/messages", "ApiKey", "{\r\n                        \"type\": \"object\",\r\n                        \"properties\": {\r\n                            \"apiKey\": {\r\n                                \"type\": \"string\",\r\n                                \"description\": \"Anthropic API Key\"\r\n                            }\r\n                        },\r\n                        \"required\": [\"apiKey\"]\r\n                    }", new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(2457), "Anthropic's Claude models for text generation", "", true, "Anthropic", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FirstName", "IsActive", "LastLoginAt", "LastName", "OwnedSessionIds", "PasswordHash", "Roles", "Username" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@mcpserver.com", null, true, new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "[]", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P/sr5iWDk3SD5AktdojZ3zZSNz39BlOjgC7pE+tNvVLK9Hdl7eVkMmb3r07FHQNpLxRWVAdJtKjZ0+tS2IY3GWY=", "[\"Admin\",\"User\"]", "admin" });

            migrationBuilder.InsertData(
                table: "LlmModels",
                columns: new[] { "Id", "ContextWindow", "CostPer1KInputTokens", "CostPer1KOutputTokens", "CreatedAt", "Description", "IsEnabled", "MaxTokens", "ModelId", "Name", "ProviderId", "SupportsStreaming", "SupportsTools", "SupportsVision", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 128000, 0.005m, 0.015m, new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(1565), "OpenAI's most capable model for text, vision, and audio tasks", true, 4096, "gpt-4o", "GPT-4o", 1, true, true, true, null },
                    { 2, 128000, 0.01m, 0.03m, new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(2388), "OpenAI's most capable model optimized for speed", true, 4096, "gpt-4-turbo", "GPT-4 Turbo", 1, true, true, true, null },
                    { 3, 16385, 0.0005m, 0.0015m, new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(2391), "OpenAI's fastest and most cost-effective model", true, 4096, "gpt-3.5-turbo", "GPT-3.5 Turbo", 1, true, true, false, null },
                    { 4, 200000, 0.015m, 0.075m, new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(2468), "Anthropic's most powerful model for highly complex tasks", true, 4096, "claude-3-opus-20240229", "Claude 3 Opus", 2, true, true, true, null },
                    { 5, 200000, 0.003m, 0.015m, new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(2471), "Anthropic's balanced model for most tasks", true, 4096, "claude-3-sonnet-20240229", "Claude 3 Sonnet", 2, true, true, true, null },
                    { 6, 200000, 0.00025m, 0.00125m, new DateTime(2025, 5, 3, 3, 30, 39, 169, DateTimeKind.Utc).AddTicks(2473), "Anthropic's fastest and most cost-effective model", true, 4096, "claude-3-haiku-20240307", "Claude 3 Haiku", 2, true, true, true, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiLogs_Timestamp",
                table: "ApiLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiLogs_UserId",
                table: "ApiLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsageLogs_ModelId",
                table: "ChatUsageLogs",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsageLogs_ProviderId",
                table: "ChatUsageLogs",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsageLogs_SessionId",
                table: "ChatUsageLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsageLogs_Timestamp",
                table: "ChatUsageLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChatUsageLogs_UserId",
                table: "ChatUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_DocumentId",
                table: "Chunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Timestamp",
                table: "ErrorLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LlmModels_ProviderId_ModelId",
                table: "LlmModels",
                columns: new[] { "ProviderId", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviderCredentials_ProviderId_UserId_Name",
                table: "LlmProviderCredentials",
                columns: new[] { "ProviderId", "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviderCredentials_UserId",
                table: "LlmProviderCredentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmProviders_Name",
                table: "LlmProviders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageLogs_CredentialId",
                table: "LlmUsageLogs",
                column: "CredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageLogs_ModelId",
                table: "LlmUsageLogs",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageLogs_Timestamp",
                table: "LlmUsageLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_LlmUsageLogs_UserId",
                table: "LlmUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                table: "Sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageMetrics_MetricType",
                table: "UsageMetrics",
                column: "MetricType");

            migrationBuilder.CreateIndex(
                name: "IX_UsageMetrics_Timestamp",
                table: "UsageMetrics",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UsageMetrics_UserId",
                table: "UsageMetrics",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiLogs");

            migrationBuilder.DropTable(
                name: "ChatUsageLogs");

            migrationBuilder.DropTable(
                name: "Chunks");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "LlmUsageLogs");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "UsageMetrics");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "LlmModels");

            migrationBuilder.DropTable(
                name: "LlmProviderCredentials");

            migrationBuilder.DropTable(
                name: "LlmProviders");
        }
    }
}
