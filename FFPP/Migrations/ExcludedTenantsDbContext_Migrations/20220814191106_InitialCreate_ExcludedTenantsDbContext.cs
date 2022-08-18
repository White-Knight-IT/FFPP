using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FFPP.Migrations.ExcludedTenantsDbContext_Migrations
{
    public partial class InitialCreate_ExcludedTenantsDbContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "_excludedTenantEntries",
                columns: table => new
                {
                    TenantDefaultDomain = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateString = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__excludedTenantEntries", x => x.TenantDefaultDomain);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_excludedTenantEntries");
        }
    }
}
