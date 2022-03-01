using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class StoreNegativeBonusValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"PetBonuses\" SET \"Value\" = 0-\"Value\" WHERE \"Value\" > 0 AND (\"BonusType\" = 16 OR \"BonusType\" = 32);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"PetBonuses\" SET \"Value\" = 0-\"Value\" WHERE \"Value\" < 0 AND (\"BonusType\" = 16 OR \"BonusType\" = 32);");
        }
    }
}
