using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261125020359_AddIdempotencyRecords : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdempotencyRecords",
            columns: table => new
            {
                Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                RequestHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                Location = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                Body = table.Column<byte[]>(type: "BLOB", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyRecords", x => x.Key);
            });

        migrationBuilder.CreateIndex(
            name: "IX_IdempotencyRecords_ExpiresAtUtc",
            table: "IdempotencyRecords",
            column: "ExpiresAtUtc");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "IdempotencyRecords");
    }
}
