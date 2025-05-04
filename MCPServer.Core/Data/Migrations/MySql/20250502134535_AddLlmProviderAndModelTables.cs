using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace MCPServer.Data.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddLlmProviderAndModelTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmUsageLogs");

            migrationBuilder.DropTable(
                name: "LlmModels");

            migrationBuilder.DropTable(
                name: "LlmProviderCredentials");

            migrationBuilder.DropTable(
                name: "LlmProviders");
        }
    }
}
