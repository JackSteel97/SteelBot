using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SteelBot.Migrations
{
    public partial class SwitchesAwayFromTimespans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeSpentDeafened",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentInVoice",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentMuted",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentStreaming",
                table: "Users");

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentDeafenedSeconds",
                table: "Users",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentInVoiceSeconds",
                table: "Users",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentMutedSeconds",
                table: "Users",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeSpentStreamingSeconds",
                table: "Users",
                type: "decimal(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeSpentDeafenedSeconds",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentInVoiceSeconds",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentMutedSeconds",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeSpentStreamingSeconds",
                table: "Users");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeSpentDeafened",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeSpentInVoice",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeSpentMuted",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeSpentStreaming",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
