using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class AddsRankRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RankRoles",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    GuildRowId = table.Column<long>(nullable: false),
                    LevelRequired = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RankRoles", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_RankRoles_Guilds_GuildRowId",
                        column: x => x.GuildRowId,
                        principalTable: "Guilds",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RankRoles_GuildRowId",
                table: "RankRoles",
                column: "GuildRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RankRoles");
        }
    }
}
