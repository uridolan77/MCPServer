using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MCPServer.Data.Migrations.MySql
{
    /// <inheritdoc />
    public partial class AddDisplayNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the DisplayName column to LlmProviders table
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "LlmProviders",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            // Add the ApiKey column to LlmProviderCredentials table
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "LlmProviderCredentials",
                type: "longtext",
                nullable: false,
                defaultValue: "");
            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(6496));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(7371));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(7375));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(7457));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(7460));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(7461));

            migrationBuilder.UpdateData(
                table: "LlmProviders",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(5278));

            migrationBuilder.UpdateData(
                table: "LlmProviders",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 21, 1, 407, DateTimeKind.Utc).AddTicks(7445));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 438, DateTimeKind.Utc).AddTicks(9393));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 439, DateTimeKind.Utc).AddTicks(551));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 439, DateTimeKind.Utc).AddTicks(555));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 439, DateTimeKind.Utc).AddTicks(666));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 439, DateTimeKind.Utc).AddTicks(669));

            migrationBuilder.UpdateData(
                table: "LlmModels",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 439, DateTimeKind.Utc).AddTicks(672));

            migrationBuilder.UpdateData(
                table: "LlmProviders",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 438, DateTimeKind.Utc).AddTicks(7979));

            migrationBuilder.UpdateData(
                table: "LlmProviders",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 5, 2, 20, 11, 20, 439, DateTimeKind.Utc).AddTicks(649));
        }
    }
}
