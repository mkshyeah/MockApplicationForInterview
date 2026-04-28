using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingHelper.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "Department", "Email", "HireDate", "Name", "Position", "Salary", "Status", "TerminationDate" },
                values: new object[,]
                {
                    { "0f48f0d8-00bc-4ed4-805b-4c41efdb2f42", "Engineering", "ruslan.iskakov@contoso.kz", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ruslan Iskakov", "DevOps Engineer", 900000m, "Active", null },
                    { "60a5eae8-a941-410f-b215-650222262f55", "Engineering", "daniyar.akhmetov@contoso.kz", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Daniyar Akhmetov", "Engineering Manager", 1200000m, "Active", null },
                    { "658fddf0-11f6-4c59-8622-3fa8af588e0b", "Engineering", "yerlan.tursynov@contoso.kz", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Yerlan Tursynov", "QA Engineer", 550000m, "Terminated", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "66a5203d-519d-4a64-9aa2-44dd55fa0c5a", "Engineering", "aigerim.nurlanova@contoso.kz", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Aigerim Nurlanova", "Senior Software Engineer", 85000m, "Active", null },
                    { "8aaeff3d-e216-49d7-9e03-8f46a3f82a7f", "HR", "aliya.bekova@contoso.kz", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Aliya Bekova", "HR Specialist", 600000m, "OnLeave", null },
                    { "a93249a9-842f-4b55-b594-f49711dfb7fd", "Design", "madina.serikova@contoso.kz", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Madina Serikova", "Product Designer", 700000m, "Active", null }
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
