using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class AddsHiddenRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Hidden",
                table: "SelfRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hidden",
                table: "SelfRoles");
        }
    }
}