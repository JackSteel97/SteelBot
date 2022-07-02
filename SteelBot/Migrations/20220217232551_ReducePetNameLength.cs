using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations;

public partial class ReducePetNameLength : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Pets",
            type: "character varying(70)",
            maxLength: 70,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Name",
            table: "Pets",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(70)",
            oldMaxLength: 70,
            oldNullable: true);
    }
}