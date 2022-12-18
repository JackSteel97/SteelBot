using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SteelBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    RowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Who = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    WhoName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    What = table.Column<string>(type: "text", nullable: false),
                    WhereGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    WhereGuildName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    WhereChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    WhereChannelName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    When = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.RowId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");
        }
    }
}
