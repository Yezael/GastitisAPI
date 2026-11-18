using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gastitis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCategoryNamePerOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                CREATE UNIQUE INDEX "IX_Categories_OwnerUserId_NormalizedName"
                ON "Categories" ("OwnerUserId", lower(btrim("Name")))
                WHERE "OwnerUserId" IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""DROP INDEX "IX_Categories_OwnerUserId_NormalizedName";""");
        }
    }
}
