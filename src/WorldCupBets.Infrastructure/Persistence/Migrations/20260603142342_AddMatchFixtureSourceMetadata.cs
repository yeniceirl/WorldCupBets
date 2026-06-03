using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchFixtureSourceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "matches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceMatchId",
                table: "matches",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceProvider",
                table: "matches",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SourceSyncedAtUtc",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AwayTeamName", "GroupName", "HomeTeamName", "SourceMatchId", "SourceProvider", "SourceSyncedAtUtc", "StartsAtUtc", "Venue" },
                values: new object[] { "South Africa", "A", "Mexico", null, null, null, new DateTime(2026, 6, 11, 18, 0, 0, 0, DateTimeKind.Utc), "Estadio Azteca" });

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AwayTeamName", "GroupName", "HomeTeamName", "SourceMatchId", "SourceProvider", "SourceSyncedAtUtc", "StartsAtUtc" },
                values: new object[] { "Czech Republic", "A", "South Korea", null, null, null, new DateTime(2026, 6, 12, 1, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AwayTeamName", "GroupName", "HomeTeamName", "SourceMatchId", "SourceProvider", "SourceSyncedAtUtc", "StartsAtUtc", "Venue" },
                values: new object[] { "Mexico", "A", "Czech Republic", null, null, null, new DateTime(2026, 6, 24, 23, 0, 0, 0, DateTimeKind.Utc), "Estadio Azteca" });

            migrationBuilder.CreateIndex(
                name: "IX_matches_Stage_GroupName_HomeTeamName_AwayTeamName",
                table: "matches",
                columns: new[] { "Stage", "GroupName", "HomeTeamName", "AwayTeamName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_matches_Stage_GroupName_HomeTeamName_AwayTeamName",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "SourceMatchId",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "SourceProvider",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "SourceSyncedAtUtc",
                table: "matches");

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AwayTeamName", "HomeTeamName", "StartsAtUtc", "Venue" },
                values: new object[] { "Japan", "Argentina", new DateTime(2026, 6, 14, 18, 0, 0, 0, DateTimeKind.Utc), "MetLife Stadium" });

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AwayTeamName", "HomeTeamName", "StartsAtUtc" },
                values: new object[] { "Mexico", "Spain", new DateTime(2026, 6, 15, 21, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AwayTeamName", "HomeTeamName", "StartsAtUtc", "Venue" },
                values: new object[] { "France", "United States", new DateTime(2026, 6, 16, 1, 0, 0, 0, DateTimeKind.Utc), "AT&T Stadium" });
        }
    }
}
