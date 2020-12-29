using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class AddsBotVotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BadBotVotes",
                table: "Guilds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GoodBotVotes",
                table: "Guilds",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadBotVotes",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "GoodBotVotes",
                table: "Guilds");
        }
    }
}
