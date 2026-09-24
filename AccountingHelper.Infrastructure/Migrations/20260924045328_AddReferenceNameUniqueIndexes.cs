using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceNameUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_positions_title",
                table: "positions",
                column: "title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_departments_name",
                table: "departments",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_positions_title",
                table: "positions");

            migrationBuilder.DropIndex(
                name: "ix_departments_name",
                table: "departments");
        }
    }
}
