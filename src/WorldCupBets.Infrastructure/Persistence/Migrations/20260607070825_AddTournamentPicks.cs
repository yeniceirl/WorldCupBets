using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentPicks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tournament_picks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SelectedText = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StakeAmountCc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PlacedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournament_picks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tournament_picks_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournament_picks_UserId_Category",
                table: "tournament_picks",
                columns: new[] { "UserId", "Category" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "tournament_picks" ("UserId", "Category", "SelectedText", "ExternalId", "StakeAmountCc", "PlacedAtUtc")
                SELECT "UserId", 'Champion', "TeamName", NULL, "StakeAmountCc", "PlacedAtUtc"
                FROM "champion_bets";
                """);

            migrationBuilder.Sql("""
                INSERT INTO "tournament_picks" ("UserId", "Category", "SelectedText", "ExternalId", "StakeAmountCc", "PlacedAtUtc")
                SELECT "UserId", "Category", "PlayerName", "ExternalPlayerId", "StakeAmountCc", "PlacedAtUtc"
                FROM "special_player_bets";
                """);

            migrationBuilder.DropTable(
                name: "champion_bets");

            migrationBuilder.DropTable(
                name: "special_player_bets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "champion_bets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlacedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StakeAmountCc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TeamName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_champion_bets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_champion_bets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "special_player_bets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalPlayerId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PlacedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StakeAmountCc = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_special_player_bets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_special_player_bets_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_champion_bets_UserId",
                table: "champion_bets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_special_player_bets_UserId_Category",
                table: "special_player_bets",
                columns: new[] { "UserId", "Category" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "champion_bets" ("UserId", "TeamName", "StakeAmountCc", "PlacedAtUtc")
                SELECT "UserId", "SelectedText", "StakeAmountCc", "PlacedAtUtc"
                FROM "tournament_picks"
                WHERE "Category" = 'Champion';
                """);

            migrationBuilder.Sql("""
                INSERT INTO "special_player_bets" ("UserId", "Category", "PlayerName", "ExternalPlayerId", "StakeAmountCc", "PlacedAtUtc")
                SELECT "UserId", "Category", "SelectedText", "ExternalId", "StakeAmountCc", "PlacedAtUtc"
                FROM "tournament_picks"
                WHERE "Category" IN ('BestPlayer', 'TopScorer');
                """);

            migrationBuilder.DropTable(
                name: "tournament_picks");
        }
    }
}
