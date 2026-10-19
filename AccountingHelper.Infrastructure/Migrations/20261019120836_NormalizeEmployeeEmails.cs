using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountingHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeEmployeeEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddBulkImport made ix_employees_email unique, but only new writes go through EmailNormalizer.
            // A row stored before it as "Ivan@x.com" would be missed by ExistsByEmailAsync("ivan@x.com") and
            // not stopped by the case-sensitive index either — two people with one identity. This brings old
            // rows under the same rule. If two of them collapse into one address the statement fails on the
            // index, which is the right outcome: that duplicate needs a person to resolve, not a migration.
            migrationBuilder.Sql(
                "UPDATE employees SET email = lower(btrim(email)) WHERE email <> lower(btrim(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to restore: the original casing is not kept anywhere, by design.
        }
    }
}
