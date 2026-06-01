#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WorldCupBets.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260531220000_AddUserBalanceAndRuleMetadata")]
public partial class AddUserBalanceAndRuleMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CurrentBalanceCc",
            table: "users",
            type: "integer",
            nullable: false,
            defaultValue: 1000);

        migrationBuilder.AddColumn<int>(
            name: "RescueCount",
            table: "users",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "RescueDebtCc",
            table: "users",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CurrentBalanceCc",
            table: "users");

        migrationBuilder.DropColumn(
            name: "RescueCount",
            table: "users");

        migrationBuilder.DropColumn(
            name: "RescueDebtCc",
            table: "users");
    }
}
