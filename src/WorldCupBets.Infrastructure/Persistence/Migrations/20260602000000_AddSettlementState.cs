#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorldCupBets.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260602000000_AddSettlementState")]
public partial class AddSettlementState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OfficialResult",
            table: "matches",
            type: "character varying(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "SettledAtUtc",
            table: "matches",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "tournament_settlements",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                ChampionJackpotCc = table.Column<int>(type: "integer", nullable: false),
                ChampionTeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                ChampionSettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UndistributedJackpotCc = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tournament_settlements", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "tournament_settlements");

        migrationBuilder.DropColumn(
            name: "OfficialResult",
            table: "matches");

        migrationBuilder.DropColumn(
            name: "SettledAtUtc",
            table: "matches");
    }
}
