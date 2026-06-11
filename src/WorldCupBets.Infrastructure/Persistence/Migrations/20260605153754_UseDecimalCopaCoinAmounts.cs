using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseDecimalCopaCoinAmounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE users ALTER COLUMN "RescueDebtCc" TYPE numeric(18,2) USING "RescueDebtCc"::numeric(18,2);
                ALTER TABLE users ALTER COLUMN "CurrentBalanceCc" TYPE numeric(18,2) USING "CurrentBalanceCc"::numeric(18,2);
                ALTER TABLE tournament_settlements ALTER COLUMN "UndistributedJackpotCc" TYPE numeric(18,2) USING "UndistributedJackpotCc"::numeric(18,2);
                ALTER TABLE tournament_settlements ALTER COLUMN "ChampionJackpotCc" TYPE numeric(18,2) USING "ChampionJackpotCc"::numeric(18,2);
                ALTER TABLE match_bets ALTER COLUMN "StakeAmountCc" TYPE numeric(18,2) USING "StakeAmountCc"::numeric(18,2);
                ALTER TABLE champion_bets ALTER COLUMN "StakeAmountCc" TYPE numeric(18,2) USING "StakeAmountCc"::numeric(18,2);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RescueDebtCc",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "CurrentBalanceCc",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "UndistributedJackpotCc",
                table: "tournament_settlements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "ChampionJackpotCc",
                table: "tournament_settlements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "StakeAmountCc",
                table: "match_bets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "StakeAmountCc",
                table: "champion_bets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
