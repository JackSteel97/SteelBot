using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class FixRankRoleDeletion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RankRoles_Guilds_GuildRowId",
                table: "RankRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_SelfRoles_Guilds_GuildRowId",
                table: "SelfRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Triggers_Guilds_GuildRowId",
                table: "Triggers");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Guilds_GuildRowId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_RankRoles_Guilds_GuildRowId",
                table: "RankRoles",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId");

            migrationBuilder.AddForeignKey(
                name: "FK_SelfRoles_Guilds_GuildRowId",
                table: "SelfRoles",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Triggers_Guilds_GuildRowId",
                table: "Triggers",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Guilds_GuildRowId",
                table: "Users",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                table: "Users",
                column: "CurrentRankRoleRowId",
                principalTable: "RankRoles",
                principalColumn: "RowId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RankRoles_Guilds_GuildRowId",
                table: "RankRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_SelfRoles_Guilds_GuildRowId",
                table: "SelfRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Triggers_Guilds_GuildRowId",
                table: "Triggers");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Guilds_GuildRowId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                table: "Users");

            migrationBuilder.AddForeignKey(
                name: "FK_RankRoles_Guilds_GuildRowId",
                table: "RankRoles",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SelfRoles_Guilds_GuildRowId",
                table: "SelfRoles",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Triggers_Guilds_GuildRowId",
                table: "Triggers",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Guilds_GuildRowId",
                table: "Users",
                column: "GuildRowId",
                principalTable: "Guilds",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_RankRoles_CurrentRankRoleRowId",
                table: "Users",
                column: "CurrentRankRoleRowId",
                principalTable: "RankRoles",
                principalColumn: "RowId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
