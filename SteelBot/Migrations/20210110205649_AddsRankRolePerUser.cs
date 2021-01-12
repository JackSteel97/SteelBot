using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class AddsRankRolePerUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentRankRoleRowId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CurrentRankRoleRowId",
                table: "Users",
                column: "CurrentRankRoleRowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                table: "Users",
                column: "CurrentRankRoleRowId",
                principalTable: "RankRoles",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CurrentRankRoleRowId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentRankRoleRowId",
                table: "Users");
        }
    }
}
