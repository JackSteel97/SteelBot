using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SteelBot.Migrations
{
    public partial class AddsTriggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Triggers",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TriggerText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ExactMatch = table.Column<bool>(type: "bit", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuildRowId = table.Column<long>(type: "bigint", nullable: false),
                    CreatorRowId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelDiscordId = table.Column<decimal>(type: "decimal(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_Triggers_Guilds_GuildRowId",
                        column: x => x.GuildRowId,
                        principalTable: "Guilds",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Triggers_Users_CreatorRowId",
                        column: x => x.CreatorRowId,
                        principalTable: "Users",
                        principalColumn: "RowId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_CreatorRowId",
                table: "Triggers",
                column: "CreatorRowId");

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_GuildRowId",
                table: "Triggers",
                column: "GuildRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Triggers");
        }
    }
}