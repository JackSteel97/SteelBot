using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace SteelBot.Migrations;

public partial class Net6 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "VoiceStartTime",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "VideoStartTime",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "UserFirstSeen",
            table: "Users",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "StreamingStartTime",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "MutedStartTime",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastXpEarningMessage",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastMessageSent",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastActivity",
            table: "Users",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "DeafenedStartTime",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "Created",
            table: "Triggers",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "SnapshotTaken",
            table: "StockPortfolioSnapshots",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastUpdated",
            table: "StockPortfolios",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Created",
            table: "StockPortfolios",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "SelfRoles",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "RankRoles",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Polls",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastUpdated",
            table: "OwnedStocks",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Timestamp",
            table: "LoggedErrors",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "BotAddedTo",
            table: "Guilds",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastUsed",
            table: "CommandStatistics",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "VoiceStartTime",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "VideoStartTime",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "UserFirstSeen",
            table: "Users",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "StreamingStartTime",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "MutedStartTime",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastXpEarningMessage",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastMessageSent",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastActivity",
            table: "Users",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "DeafenedStartTime",
            table: "Users",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "Created",
            table: "Triggers",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "SnapshotTaken",
            table: "StockPortfolioSnapshots",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastUpdated",
            table: "StockPortfolios",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Created",
            table: "StockPortfolios",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "SelfRoles",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "RankRoles",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "Polls",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastUpdated",
            table: "OwnedStocks",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "Timestamp",
            table: "LoggedErrors",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "BotAddedTo",
            table: "Guilds",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "LastUsed",
            table: "CommandStatistics",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");
    }
}