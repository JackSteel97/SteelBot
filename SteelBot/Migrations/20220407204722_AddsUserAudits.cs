using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class AddsUserAudits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserAudits",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildDiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CurrentRankRoleName = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    MessageCount = table.Column<long>(type: "bigint", nullable: false),
                    TotalMessageLength = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentInVoiceSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentMutedSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentDeafenedSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentStreamingSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentOnVideoSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentAfkSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeSpentDisconnectedSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildRowId = table.Column<long>(type: "bigint", nullable: false),
                    MessageXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VoiceXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    MutedXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DeafenedXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StreamingXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VideoXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DisconnectedXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    CurrentRankRoleRowId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAudits", x => x.RowId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAudits");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Users");
        }
    }
}
