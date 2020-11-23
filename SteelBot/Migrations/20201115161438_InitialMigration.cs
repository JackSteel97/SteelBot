using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscordId = table.Column<decimal>(nullable: false),
                    BotAddedTo = table.Column<DateTime>(nullable: false),
                    CommandPrefix = table.Column<string>(maxLength: 20, nullable: true, defaultValue: "+")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.RowId);
                });

            migrationBuilder.CreateTable(
                name: "LoggedErrors",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    Message = table.Column<string>(nullable: true),
                    StackTrace = table.Column<string>(nullable: true),
                    SourceMethod = table.Column<string>(nullable: true),
                    FullDetail = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoggedErrors", x => x.RowId);
                });

            migrationBuilder.CreateTable(
                name: "SelfRoles",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    GuildRowId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfRoles", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_SelfRoles_Guilds_GuildRowId",
                        column: x => x.GuildRowId,
                        principalTable: "Guilds",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiscordId = table.Column<decimal>(nullable: false),
                    MessageCount = table.Column<long>(nullable: false),
                    TotalMessageLength = table.Column<decimal>(nullable: false),
                    TimeSpentInVoice = table.Column<TimeSpan>(nullable: false),
                    TimeSpentMuted = table.Column<TimeSpan>(nullable: false),
                    TimeSpentDeafened = table.Column<TimeSpan>(nullable: false),
                    TimeSpentStreaming = table.Column<TimeSpan>(nullable: false),
                    LastCommandReceived = table.Column<DateTime>(nullable: false),
                    UserFirstSeen = table.Column<DateTime>(nullable: false),
                    MutedStartTime = table.Column<DateTime>(nullable: true),
                    DeafenedStartTime = table.Column<DateTime>(nullable: true),
                    StreamingStartTime = table.Column<DateTime>(nullable: true),
                    VoiceStartTime = table.Column<DateTime>(nullable: true),
                    LastActivity = table.Column<DateTime>(nullable: false),
                    GuildRowId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_Users_Guilds_GuildRowId",
                        column: x => x.GuildRowId,
                        principalTable: "Guilds",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Polls",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    MessageId = table.Column<decimal>(nullable: false),
                    ChannelId = table.Column<decimal>(nullable: false),
                    UserRowId = table.Column<long>(nullable: false),
                    IsLockedPoll = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Polls", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_Polls_Users_UserRowId",
                        column: x => x.UserRowId,
                        principalTable: "Users",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollOptions",
                columns: table => new
                {
                    RowId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PollRowId = table.Column<long>(nullable: false),
                    OptionText = table.Column<string>(maxLength: 255, nullable: true),
                    OptionNumber = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollOptions", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_PollOptions_Polls_PollRowId",
                        column: x => x.PollRowId,
                        principalTable: "Polls",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_DiscordId",
                table: "Guilds",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollOptions_PollRowId",
                table: "PollOptions",
                column: "PollRowId");

            migrationBuilder.CreateIndex(
                name: "IX_Polls_UserRowId",
                table: "Polls",
                column: "UserRowId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfRoles_GuildRowId",
                table: "SelfRoles",
                column: "GuildRowId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GuildRowId",
                table: "Users",
                column: "GuildRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoggedErrors");

            migrationBuilder.DropTable(
                name: "PollOptions");

            migrationBuilder.DropTable(
                name: "SelfRoles");

            migrationBuilder.DropTable(
                name: "Polls");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
