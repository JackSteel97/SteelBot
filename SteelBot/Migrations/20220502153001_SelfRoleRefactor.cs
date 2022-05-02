using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class SelfRoleRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "SelfRoles");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscordRoleId",
                table: "SelfRoles",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscordRoleId",
                table: "SelfRoles");

            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "SelfRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
