using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations;

public partial class XpSeparation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ActivityXpEarned",
            table: "Users");

        migrationBuilder.AddColumn<decimal>(
           name: "VoiceXpEarned",
           table: "Users",
           type: "numeric(20,0)",
           nullable: false,
           defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "DeafenedXpEarned",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "MutedXpEarned",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "StreamingXpEarned",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "VideoXpEarned",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DeafenedXpEarned",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "MutedXpEarned",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "StreamingXpEarned",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "VideoXpEarned",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "VoiceXpEarned",
            table: "Users");

        migrationBuilder.AddColumn<decimal>(
            name: "ActivityXpEarned",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);
    }
}