using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "tournament_settlements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "match_bets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 1,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 2,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "matches",
                keyColumn: "Id",
                keyValue: 3,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 101,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 102,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 103,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 104,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 105,
                column: "Version",
                value: 0);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: 106,
                column: "Version",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "tournament_settlements");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "match_bets");
        }
    }
}
