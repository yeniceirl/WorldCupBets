using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "match_challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    ClaimText = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    CreatorSideText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TakerSideText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StakeAmountCc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WinnerSide = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_challenges_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_challenge_positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchChallengeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StakeAmountCc = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EscrowedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_challenge_positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_challenge_positions_match_challenges_MatchChallengeId",
                        column: x => x.MatchChallengeId,
                        principalTable: "match_challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_match_challenge_positions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_challenge_positions_MatchChallengeId_Side",
                table: "match_challenge_positions",
                columns: new[] { "MatchChallengeId", "Side" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_challenge_positions_UserId",
                table: "match_challenge_positions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_match_challenges_MatchId_Status",
                table: "match_challenges",
                columns: new[] { "MatchId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "match_challenge_positions");

            migrationBuilder.DropTable(
                name: "match_challenges");
        }
    }
}
