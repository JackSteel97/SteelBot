using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace SteelBot.Migrations;

public partial class AddDisconnectedTimeTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DisconnectedStartTime",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "DisconnectedXpEarned",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "TimeSpentDisconnectedSeconds",
            table: "Users",
            type: "numeric(20,0)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DisconnectedStartTime",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "DisconnectedXpEarned",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "TimeSpentDisconnectedSeconds",
            table: "Users");
    }
}