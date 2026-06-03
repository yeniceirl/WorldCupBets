using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WorldCupBets.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoLeaderboardUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CurrentBalanceCc", "DisplayName", "Email", "GoogleSubject", "RescueCount", "RescueDebtCc" },
                values: new object[,]
                {
                    { 101, 1325, "Maple Moose", "maple@worldcupbets.local", "demo-maple", 0, 0 },
                    { 102, 1180, "Zayu Jaguar", "zayu@worldcupbets.local", "demo-zayu", 0, 0 },
                    { 103, 1110, "Clutch Eagle", "clutch@worldcupbets.local", "demo-clutch", 1, 100 },
                    { 104, 990, "Lucia del Gol", "lucia@worldcupbets.local", "demo-lucia", 0, 0 },
                    { 105, 845, "Takeshi Bracket", "takeshi@worldcupbets.local", "demo-takeshi", 2, 200 },
                    { 106, 760, "Nora Finalista", "nora@worldcupbets.local", "demo-nora", 0, 0 }
                });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { 2, 101 },
                    { 2, 102 },
                    { 2, 103 },
                    { 2, 104 },
                    { 2, 105 },
                    { 2, 106 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 101 });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 102 });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 103 });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 104 });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 105 });

            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { 2, 106 });

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: 106);
        }
    }
}
