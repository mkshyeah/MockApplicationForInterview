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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    { new Guid("046e0b69-f987-4000-afad-f9b9fa2b6050"), new DateTime(2026, 4, 30, 8, 21, 45, 172, DateTimeKind.Utc).AddTicks(1910), "Engineering", "aigerim.nurlanova@contoso.kz", "Aigerim", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nurlanova", "Senior Software Engineer", 85000m, "Active", null, null },
                    { new Guid("0efb8546-825a-4692-893a-0cf3547770dd"), new DateTime(2026, 4, 30, 8, 21, 45, 172, DateTimeKind.Utc).AddTicks(1920), "Engineering", "yerlan.tursynov@contoso.kz", "Yerlan", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tursynov", "QA Engineer", 550000m, "Fired", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), null },
                    { new Guid("630a08b1-637d-465d-8718-473d987fd045"), new DateTime(2026, 4, 30, 8, 21, 45, 172, DateTimeKind.Utc).AddTicks(1930), "HR", "aliya.bekova@contoso.kz", "Aliya", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bekova", "HR Specialist", 600000m, "OnVacation", null, null },
                    { new Guid("73750a2b-5dba-4617-9363-085cc14f6e6d"), new DateTime(2026, 4, 30, 8, 21, 45, 172, DateTimeKind.Utc).AddTicks(1920), "Design", "madina.serikova@contoso.kz", "Madina", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Serikova", "Product Designer", 700000m, "Active", null, null },
                    { new Guid("ab2d8a30-d5af-4f6e-8d4e-165690b4ba35"), new DateTime(2026, 4, 30, 8, 21, 45, 172, DateTimeKind.Utc).AddTicks(1930), "Engineering", "ruslan.iskakov@contoso.kz", "Ruslan", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Iskakov", "DevOps Engineer", 900000m, "Active", null, null },
                    { new Guid("c912173c-db4a-4a87-9739-d5b82fe26400"), new DateTime(2026, 4, 30, 8, 21, 45, 172, DateTimeKind.Utc).AddTicks(1910), "Engineering", "daniyar.akhmetov@contoso.kz", "Daniyar", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Akhmetov", "Engineering Manager", 1200000m, "Active", null, null }
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
