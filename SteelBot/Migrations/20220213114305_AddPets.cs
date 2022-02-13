using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class AddPets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerDiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    EarnedXp = table.Column<double>(type: "double precision", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    BornAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FoundAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Species = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.RowId);
                });

            migrationBuilder.CreateTable(
                name: "PetAttributes",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PetId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetAttributes", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_PetAttributes_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PetBonuses",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PetId = table.Column<long>(type: "bigint", nullable: false),
                    BonusType = table.Column<int>(type: "integer", nullable: false),
                    PercentageValue = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetBonuses", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_PetBonuses_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "RowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PetAttributes_PetId",
                table: "PetAttributes",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_PetBonuses_PetId",
                table: "PetBonuses",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_OwnerDiscordId",
                table: "Pets",
                column: "OwnerDiscordId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetAttributes");

            migrationBuilder.DropTable(
                name: "PetBonuses");

            migrationBuilder.DropTable(
                name: "Pets");
        }
    }
}
