using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class AddsPortfolioSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "OwnedStocks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "StockPortfolioSnapshots",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentPortfolioRowId = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotTaken = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalValueDollars = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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
                name: "IX_StockPortfolioSnapshots_ParentPortfolioRowId",
                table: "StockPortfolioSnapshots",
                column: "ParentPortfolioRowId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockPortfolioSnapshots");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "OwnedStocks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
