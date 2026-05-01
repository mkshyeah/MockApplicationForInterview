using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingHelper.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CreatedAt", "Department", "Email", "FirstName", "HireDate", "LastName", "Position", "Salary", "Status", "TerminationDate", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering", "aigerim.nurlanova@contoso.kz", "Aigerim", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nurlanova", "Senior Software Engineer", 850000m, "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000002"), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering", "daniyar.akhmetov@contoso.kz", "Daniyar", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Akhmetov", "Engineering Manager", 1200000m, "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000003"), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Design", "madina.serikova@contoso.kz", "Madina", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Serikova", "Product Designer", 700000m, "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000004"), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering", "yerlan.tursynov@contoso.kz", "Yerlan", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tursynov", "QA Engineer", 550000m, "Fired", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), null },
                    { new Guid("11111111-0000-0000-0000-000000000005"), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "HR", "aliya.bekova@contoso.kz", "Aliya", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bekova", "HR Specialist", 600000m, "OnVacation", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000006"), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Engineering", "ruslan.iskakov@contoso.kz", "Ruslan", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Iskakov", "DevOps Engineer", 900000m, "Active", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
