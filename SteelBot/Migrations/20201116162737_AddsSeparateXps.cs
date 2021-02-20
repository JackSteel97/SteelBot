using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class AddsSeparateXps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XpEarned",
                table: "Users");

            migrationBuilder.AddColumn<decimal>(
                name: "ActivityXpEarned",
                table: "Users",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MessageXpEarned",
                table: "Users",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityXpEarned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MessageXpEarned",
                table: "Users");

            migrationBuilder.AddColumn<decimal>(
                name: "XpEarned",
                table: "Users",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}