using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastitisAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangedCapitalLetterOnDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "date",
                table: "Expenses",
                newName: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Expenses",
                newName: "date");
        }
    }
}
