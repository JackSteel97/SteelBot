using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SteelBot.Migrations
{
    public partial class AfkTiming : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AfkStartTime",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentAfkSeconds",
                table: "Users",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfkStartTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentAfkSeconds",
                table: "Users");
        }
    }
}
