using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficialMatchData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfficialAwayScore",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialDataProvider",
                table: "matches",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficialDataSourceReference",
                table: "matches",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfficialDataVerifiedAtUtc",
                table: "matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfficialHomeScore",
                table: "matches",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "OfficialAwayScore", "OfficialDataProvider", "OfficialDataSourceReference", "OfficialDataVerifiedAtUtc", "OfficialHomeScore" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "OfficialAwayScore", "OfficialDataProvider", "OfficialDataSourceReference", "OfficialDataVerifiedAtUtc", "OfficialHomeScore" },
                values: new object[] { null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "OfficialAwayScore", "OfficialDataProvider", "OfficialDataSourceReference", "OfficialDataVerifiedAtUtc", "OfficialHomeScore" },
                values: new object[] { null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfficialAwayScore",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OfficialDataProvider",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OfficialDataSourceReference",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OfficialDataVerifiedAtUtc",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "OfficialHomeScore",
                table: "matches");
        }
    }
}
