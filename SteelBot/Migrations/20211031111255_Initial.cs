using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

namespace SteelBot.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CommandStatistics",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CommandName = table.Column<string>(type: "text", nullable: true),
                UsageCount = table.Column<long>(type: "bigint", nullable: false),
                LastUsed = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CommandStatistics", x => x.RowId));

        migrationBuilder.CreateTable(
            name: "Guilds",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                BotAddedTo = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                CommandPrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, defaultValue: "+"),
                LevelAnnouncementChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                GoodBotVotes = table.Column<int>(type: "integer", nullable: false),
                BadBotVotes = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Guilds", x => x.RowId));

        migrationBuilder.CreateTable(
            name: "LoggedErrors",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                Message = table.Column<string>(type: "text", nullable: true),
                StackTrace = table.Column<string>(type: "text", nullable: true),
                SourceMethod = table.Column<string>(type: "text", nullable: true),
                FullDetail = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_LoggedErrors", x => x.RowId));

        migrationBuilder.CreateTable(
            name: "RankRoles",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RoleName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                GuildRowId = table.Column<long>(type: "bigint", nullable: false),
                LevelRequired = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RankRoles", x => x.RowId);
                table.ForeignKey(
                    name: "FK_RankRoles_Guilds_GuildRowId",
                    column: x => x.GuildRowId,
                    principalTable: "Guilds",
                    principalColumn: "RowId");
            });

        migrationBuilder.CreateTable(
            name: "SelfRoles",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RoleName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                Hidden = table.Column<bool>(type: "boolean", nullable: false),
                GuildRowId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SelfRoles", x => x.RowId);
                table.ForeignKey(
                    name: "FK_SelfRoles_Guilds_GuildRowId",
                    column: x => x.GuildRowId,
                    principalTable: "Guilds",
                    principalColumn: "RowId");
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                MessageCount = table.Column<long>(type: "bigint", nullable: false),
                TotalMessageLength = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                TimeSpentInVoiceSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                TimeSpentMutedSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                TimeSpentDeafenedSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                TimeSpentStreamingSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                TimeSpentOnVideoSeconds = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                UserFirstSeen = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                MutedStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                DeafenedStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                StreamingStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                VideoStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                VoiceStartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                LastActivity = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                GuildRowId = table.Column<long>(type: "bigint", nullable: false),
                MessageXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                ActivityXpEarned = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                LastMessageSent = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                LastXpEarningMessage = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CurrentRankRoleRowId = table.Column<long>(type: "bigint", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.RowId);
                table.ForeignKey(
                    name: "FK_Users_Guilds_GuildRowId",
                    column: x => x.GuildRowId,
                    principalTable: "Guilds",
                    principalColumn: "RowId");
                table.ForeignKey(
                    name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                    column: x => x.CurrentRankRoleRowId,
                    principalTable: "RankRoles",
                    principalColumn: "RowId",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                UserRowId = table.Column<long>(type: "bigint", nullable: false),
                IsLockedPoll = table.Column<bool>(type: "boolean", nullable: false)
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
            name: "StockPortfolios",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OwnerRowId = table.Column<long>(type: "bigint", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockPortfolios", x => x.RowId);
                table.ForeignKey(
                    name: "FK_StockPortfolios_Users_OwnerRowId",
                    column: x => x.OwnerRowId,
                    principalTable: "Users",
                    principalColumn: "RowId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Triggers",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TriggerText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                ExactMatch = table.Column<bool>(type: "boolean", nullable: false),
                Response = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                GuildRowId = table.Column<long>(type: "bigint", nullable: false),
                CreatorRowId = table.Column<long>(type: "bigint", nullable: false),
                ChannelDiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                TimesActivated = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Triggers", x => x.RowId);
                table.ForeignKey(
                    name: "FK_Triggers_Guilds_GuildRowId",
                    column: x => x.GuildRowId,
                    principalTable: "Guilds",
                    principalColumn: "RowId");
                table.ForeignKey(
                    name: "FK_Triggers_Users_CreatorRowId",
                    column: x => x.CreatorRowId,
                    principalTable: "Users",
                    principalColumn: "RowId");
            });

        migrationBuilder.CreateTable(
            name: "PollOptions",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PollRowId = table.Column<long>(type: "bigint", nullable: false),
                OptionText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                OptionNumber = table.Column<int>(type: "integer", nullable: false)
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

        migrationBuilder.CreateTable(
            name: "OwnedStocks",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ParentPortfolioRowId = table.Column<long>(type: "bigint", nullable: false),
                Symbol = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AmountOwned = table.Column<decimal>(type: "numeric(38,20)", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OwnedStocks", x => x.RowId);
                table.ForeignKey(
                    name: "FK_OwnedStocks_StockPortfolios_ParentPortfolioRowId",
                    column: x => x.ParentPortfolioRowId,
                    principalTable: "StockPortfolios",
                    principalColumn: "RowId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "StockPortfolioSnapshots",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ParentPortfolioRowId = table.Column<long>(type: "bigint", nullable: false),
                SnapshotTaken = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                TotalValueDollars = table.Column<decimal>(type: "numeric(24,4)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockPortfolioSnapshots", x => x.RowId);
                table.ForeignKey(
                    name: "FK_StockPortfolioSnapshots_StockPortfolios_ParentPortfolioRowId",
                    column: x => x.ParentPortfolioRowId,
                    principalTable: "StockPortfolios",
                    principalColumn: "RowId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CommandStatistics_CommandName",
            table: "CommandStatistics",
            column: "CommandName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Guilds_DiscordId",
            table: "Guilds",
            column: "DiscordId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OwnedStocks_ParentPortfolioRowId",
            table: "OwnedStocks",
            column: "ParentPortfolioRowId");

        migrationBuilder.CreateIndex(
            name: "IX_PollOptions_PollRowId",
            table: "PollOptions",
            column: "PollRowId");

        migrationBuilder.CreateIndex(
            name: "IX_Polls_UserRowId",
            table: "Polls",
            column: "UserRowId");

        migrationBuilder.CreateIndex(
            name: "IX_RankRoles_GuildRowId",
            table: "RankRoles",
            column: "GuildRowId");

        migrationBuilder.CreateIndex(
            name: "IX_SelfRoles_GuildRowId",
            table: "SelfRoles",
            column: "GuildRowId");

        migrationBuilder.CreateIndex(
            name: "IX_StockPortfolios_OwnerRowId",
            table: "StockPortfolios",
            column: "OwnerRowId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StockPortfolioSnapshots_ParentPortfolioRowId",
            table: "StockPortfolioSnapshots",
            column: "ParentPortfolioRowId");

        migrationBuilder.CreateIndex(
            name: "IX_Triggers_CreatorRowId",
            table: "Triggers",
            column: "CreatorRowId");

        migrationBuilder.CreateIndex(
            name: "IX_Triggers_GuildRowId",
            table: "Triggers",
            column: "GuildRowId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_CurrentRankRoleRowId",
            table: "Users",
            column: "CurrentRankRoleRowId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_GuildRowId",
            table: "Users",
            column: "GuildRowId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CommandStatistics");

        migrationBuilder.DropTable(
            name: "LoggedErrors");

        migrationBuilder.DropTable(
            name: "OwnedStocks");

        migrationBuilder.DropTable(
            name: "PollOptions");

        migrationBuilder.DropTable(
            name: "SelfRoles");

        migrationBuilder.DropTable(
            name: "StockPortfolioSnapshots");

        migrationBuilder.DropTable(
            name: "Triggers");

        migrationBuilder.DropTable(
            name: "Polls");

        migrationBuilder.DropTable(
            name: "StockPortfolios");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "RankRoles");

        migrationBuilder.DropTable(
            name: "Guilds");
    }
}