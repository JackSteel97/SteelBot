using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SteelBot.Migrations
{
    public partial class AddsCommandStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandStatistics",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandStatistics", x => x.RowId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandStatistics_CommandName",
                table: "CommandStatistics",
                column: "CommandName",
                unique: true,
                filter: "[CommandName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandStatistics");
        }
    }
}