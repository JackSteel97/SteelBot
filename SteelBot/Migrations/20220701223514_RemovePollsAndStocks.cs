using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using System;

#nullable disable

namespace SteelBot.Migrations;

public partial class RemovePollsAndStocks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OwnedStocks");

        migrationBuilder.DropTable(
            name: "PollOptions");

        migrationBuilder.DropTable(
            name: "StockPortfolioSnapshots");

        migrationBuilder.DropTable(
            name: "Polls");

        migrationBuilder.DropTable(
            name: "StockPortfolios");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserRowId = table.Column<long>(type: "bigint", nullable: false),
                ChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsLockedPoll = table.Column<bool>(type: "boolean", nullable: false),
                MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
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
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
            name: "PollOptions",
            columns: table => new
            {
                RowId = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                PollRowId = table.Column<long>(type: "bigint", nullable: false),
                OptionNumber = table.Column<int>(type: "integer", nullable: false),
                OptionText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
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
                AmountOwned = table.Column<decimal>(type: "numeric(38,20)", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Symbol = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
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
                SnapshotTaken = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
            name: "IX_StockPortfolios_OwnerRowId",
            table: "StockPortfolios",
            column: "OwnerRowId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StockPortfolioSnapshots_ParentPortfolioRowId",
            table: "StockPortfolioSnapshots",
            column: "ParentPortfolioRowId");
    }
}