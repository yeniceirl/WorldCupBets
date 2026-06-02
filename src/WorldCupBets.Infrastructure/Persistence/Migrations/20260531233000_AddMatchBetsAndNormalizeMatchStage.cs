#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WorldCupBets.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260531233000_AddMatchBetsAndNormalizeMatchStage")]
public partial class AddMatchBetsAndNormalizeMatchStage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE matches
            SET "Stage" = CASE "Stage"
                WHEN 'Group Stage' THEN 'GroupStage'
                WHEN 'Round of 32' THEN 'RoundOf32'
                WHEN 'Round of 16' THEN 'RoundOf16'
                WHEN 'Quarterfinals' THEN 'Quarterfinals'
                WHEN 'Semifinals' THEN 'Semifinals'
                WHEN 'Third Place' THEN 'ThirdPlace'
                WHEN 'Final' THEN 'Final'
                ELSE "Stage"
            END;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Stage",
            table: "matches",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100);

        migrationBuilder.CreateTable(
            name: "match_bets",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                MatchId = table.Column<int>(type: "integer", nullable: false),
                Selection = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                StakeAmountCc = table.Column<int>(type: "integer", nullable: false),
                PlacedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_match_bets", x => x.Id);
                table.ForeignKey(
                    name: "FK_match_bets_matches_MatchId",
                    column: x => x.MatchId,
                    principalTable: "matches",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_match_bets_users_UserId",
                    column: x => x.UserId,
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_match_bets_MatchId",
            table: "match_bets",
            column: "MatchId");

        migrationBuilder.CreateIndex(
            name: "IX_match_bets_UserId_MatchId",
            table: "match_bets",
            columns: new[] { "UserId", "MatchId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "match_bets");

        migrationBuilder.AlterColumn<string>(
            name: "Stage",
            table: "matches",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(32)",
            oldMaxLength: 32);

        migrationBuilder.Sql("""
            UPDATE matches
            SET "Stage" = CASE "Stage"
                WHEN 'GroupStage' THEN 'Group Stage'
                WHEN 'RoundOf32' THEN 'Round of 32'
                WHEN 'RoundOf16' THEN 'Round of 16'
                WHEN 'Quarterfinals' THEN 'Quarterfinals'
                WHEN 'Semifinals' THEN 'Semifinals'
                WHEN 'ThirdPlace' THEN 'Third Place'
                WHEN 'Final' THEN 'Final'
                ELSE "Stage"
            END;
            """);
    }
}
