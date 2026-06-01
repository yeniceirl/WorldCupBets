#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WorldCupBets.Infrastructure.Persistence.Migrations;

public partial class AddMatches : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "matches",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                HomeTeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AwayTeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Venue = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_matches", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "matches",
            columns: new[] { "Id", "AwayTeamName", "HomeTeamName", "Stage", "StartsAtUtc", "Venue" },
            values: new object[,]
            {
                { 1, "Japan", "Argentina", "Group Stage", new DateTime(2026, 6, 14, 18, 0, 0, DateTimeKind.Utc), "MetLife Stadium" },
                { 2, "Mexico", "Spain", "Group Stage", new DateTime(2026, 6, 15, 21, 0, 0, DateTimeKind.Utc), "Estadio Akron" },
                { 3, "France", "United States", "Group Stage", new DateTime(2026, 6, 16, 1, 0, 0, DateTimeKind.Utc), "AT&T Stadium" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "matches");
    }
}
