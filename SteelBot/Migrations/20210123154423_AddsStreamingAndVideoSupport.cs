using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SteelBot.Migrations
{
    public partial class AddsStreamingAndVideoSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentOnVideoSeconds",
                table: "Users",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "VideoStartTime",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeSpentOnVideoSeconds",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "VideoStartTime",
                table: "Users");
        }
    }
}