#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace WorldCupBets.Infrastructure.Persistence.Migrations;

public partial class ScaffoldBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "lookup_items",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Detail = table.Column<string>(type: "text", nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_lookup_items", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_lookup_items_Category_Key",
            table: "lookup_items",
            columns: new[] { "Category", "Key" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "lookup_items");
    }
}
