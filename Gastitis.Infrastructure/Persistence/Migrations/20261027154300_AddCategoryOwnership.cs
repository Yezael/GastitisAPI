using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gastitis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -7,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -6,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -5,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -4,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -3,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -2,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: -1,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OwnerUserId",
                table: "Categories",
                column: "OwnerUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Categories_Ownership",
                table: "Categories",
                sql: "(\"SystemCode\" IS NOT NULL AND \"OwnerUserId\" IS NULL)\r\nOR\r\n(\"SystemCode\" IS NULL AND \"OwnerUserId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Users_OwnerUserId",
                table: "Categories",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Users_OwnerUserId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_OwnerUserId",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Categories_Ownership",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Categories");
        }
    }
}
