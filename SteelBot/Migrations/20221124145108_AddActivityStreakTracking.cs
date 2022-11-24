using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class AddActivityStreakTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActivityStreakXpEarned",
                table: "Users",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveDaysActive",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastActiveDay",
                table: "Users",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<decimal>(
                name: "ActivityStreakXpEarned",
                table: "UserAudits",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveDaysActive",
                table: "UserAudits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastActiveDay",
                table: "UserAudits",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivityStreakXpEarned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConsecutiveDaysActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastActiveDay",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActivityStreakXpEarned",
                table: "UserAudits");

            migrationBuilder.DropColumn(
                name: "ConsecutiveDaysActive",
                table: "UserAudits");

            migrationBuilder.DropColumn(
                name: "LastActiveDay",
                table: "UserAudits");
        }
    }
}
