using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class ChangeBonusValueName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PercentageValue",
                table: "PetBonuses",
                newName: "Value");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "PetBonuses",
                newName: "PercentageValue");
        }
    }
}
