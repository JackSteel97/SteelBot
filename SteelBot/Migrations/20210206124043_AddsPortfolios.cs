using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SteelBot.Migrations
{
    public partial class AddsPortfolios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockPortfolios",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerRowId = table.Column<long>(type: "bigint", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "OwnedStocks",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentPortfolioRowId = table.Column<long>(type: "bigint", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AmountOwned = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_OwnedStocks_ParentPortfolioRowId",
                table: "OwnedStocks",
                column: "ParentPortfolioRowId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPortfolios_OwnerRowId",
                table: "StockPortfolios",
                column: "OwnerRowId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnedStocks");

            migrationBuilder.DropTable(
                name: "StockPortfolios");
        }
    }
}