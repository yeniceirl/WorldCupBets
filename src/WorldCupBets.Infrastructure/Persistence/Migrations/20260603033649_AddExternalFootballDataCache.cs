using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalFootballDataCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_football_group_standings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GroupName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TeamExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Played = table.Column<int>(type: "integer", nullable: false),
                    Won = table.Column<int>(type: "integer", nullable: false),
                    Drawn = table.Column<int>(type: "integer", nullable: false),
                    Lost = table.Column<int>(type: "integer", nullable: false),
                    GoalsFor = table.Column<int>(type: "integer", nullable: false),
                    GoalsAgainst = table.Column<int>(type: "integer", nullable: false),
                    GoalDifference = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_football_group_standings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "external_football_matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HomeTeamExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AwayTeamExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    HomeTeamNameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AwayTeamNameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HomeTeamLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AwayTeamLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    GroupName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Matchday = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LocalDateText = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StadiumExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsFinished = table.Column<bool>(type: "boolean", nullable: false),
                    TimeElapsed = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StageType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_football_matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "external_football_stadiums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FifaName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CityEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CountryEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    Region = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_football_stadiums", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "external_football_teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FifaCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Iso2 = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GroupName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FlagUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_football_teams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_football_group_standings_ProviderName_GroupName_Te~",
                table: "external_football_group_standings",
                columns: new[] { "ProviderName", "GroupName", "TeamExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_football_matches_ProviderName_ExternalId",
                table: "external_football_matches",
                columns: new[] { "ProviderName", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_football_stadiums_ProviderName_ExternalId",
                table: "external_football_stadiums",
                columns: new[] { "ProviderName", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_football_teams_ProviderName_ExternalId",
                table: "external_football_teams",
                columns: new[] { "ProviderName", "ExternalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_football_group_standings");

            migrationBuilder.DropTable(
                name: "external_football_matches");

            migrationBuilder.DropTable(
                name: "external_football_stadiums");

            migrationBuilder.DropTable(
                name: "external_football_teams");
        }
    }
}
