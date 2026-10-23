using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gastitis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addexpenseOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM "Expenses"; """);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Expenses",
                type: "integer",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OwnerUserId",
                table: "Expenses",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_OwnerUserId",
                table: "Expenses",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_OwnerUserId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_OwnerUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Expenses");
        }
    }
}
