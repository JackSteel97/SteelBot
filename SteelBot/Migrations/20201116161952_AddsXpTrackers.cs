using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace SteelBot.Migrations
{
    public partial class AddsXpTrackers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentLevel",
                table: "Users",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageSent",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "XpEarned",
                table: "Users",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LevelAnnouncementChannelId",
                table: "Guilds",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastMessageSent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "XpEarned",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LevelAnnouncementChannelId",
                table: "Guilds");
        }
    }
}