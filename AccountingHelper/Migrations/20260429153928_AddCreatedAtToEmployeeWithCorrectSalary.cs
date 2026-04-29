using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingHelper.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToEmployeeWithCorrectSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("224f409b-c60e-43a8-aad7-e78134ce5130"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("6f891cfe-5f0d-4997-926d-6928702147e1"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("7df96215-6563-403b-97e2-781e7b65b50a"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b1cb6b2f-8277-4896-a0bd-72e10d9733ff"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("cbd36a56-9cdd-4911-bb83-0ffe6e6781b8"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("e9ef064f-48d9-43da-86b9-47e5dd32f76f"));

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CreatedAt", "Department", "Email", "FirstName", "HireDate", "LastName", "Position", "Salary", "Status", "TerminationDate", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("5a13e76a-eec1-4095-9aca-22be39538931"), new DateTime(2026, 4, 29, 15, 39, 27, 958, DateTimeKind.Utc).AddTicks(6870), "HR", "aliya.bekova@contoso.kz", "Aliya", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bekova", "HR Specialist", 600000m, "OnVacation", null, null },
                    { new Guid("7b8d4444-1311-4b96-b00c-c2f3b09405a8"), new DateTime(2026, 4, 29, 15, 39, 27, 958, DateTimeKind.Utc).AddTicks(6870), "Engineering", "yerlan.tursynov@contoso.kz", "Yerlan", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tursynov", "QA Engineer", 550000m, "Fired", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), null },
                    { new Guid("9c8f44b6-6c39-43d3-933e-fce7584b1530"), new DateTime(2026, 4, 29, 15, 39, 27, 958, DateTimeKind.Utc).AddTicks(6880), "Engineering", "ruslan.iskakov@contoso.kz", "Ruslan", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Iskakov", "DevOps Engineer", 900000m, "Active", null, null },
                    { new Guid("b44a5c2d-ebdb-4239-9b68-ca3837100840"), new DateTime(2026, 4, 29, 15, 39, 27, 958, DateTimeKind.Utc).AddTicks(6860), "Engineering", "aigerim.nurlanova@contoso.kz", "Aigerim", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nurlanova", "Senior Software Engineer", 85000m, "Active", null, null },
                    { new Guid("f2ee2ca0-48cf-449e-addb-e2cc38048a89"), new DateTime(2026, 4, 29, 15, 39, 27, 958, DateTimeKind.Utc).AddTicks(6870), "Design", "madina.serikova@contoso.kz", "Madina", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Serikova", "Product Designer", 700000m, "Active", null, null },
                    { new Guid("ff5e3e20-4fdf-4946-b4ed-884bc51c44e4"), new DateTime(2026, 4, 29, 15, 39, 27, 958, DateTimeKind.Utc).AddTicks(6860), "Engineering", "daniyar.akhmetov@contoso.kz", "Daniyar", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Akhmetov", "Engineering Manager", 1200000m, "Active", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("5a13e76a-eec1-4095-9aca-22be39538931"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("7b8d4444-1311-4b96-b00c-c2f3b09405a8"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("9c8f44b6-6c39-43d3-933e-fce7584b1530"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("b44a5c2d-ebdb-4239-9b68-ca3837100840"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("f2ee2ca0-48cf-449e-addb-e2cc38048a89"));

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: new Guid("ff5e3e20-4fdf-4946-b4ed-884bc51c44e4"));

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CreatedAt", "Department", "Email", "FirstName", "HireDate", "LastName", "Position", "Salary", "Status", "TerminationDate", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("224f409b-c60e-43a8-aad7-e78134ce5130"), new DateTime(2026, 4, 29, 15, 37, 40, 538, DateTimeKind.Utc).AddTicks(1250), "Engineering", "yerlan.tursynov@contoso.kz", "Yerlan", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tursynov", "QA Engineer", 550000m, "Fired", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), null },
                    { new Guid("6f891cfe-5f0d-4997-926d-6928702147e1"), new DateTime(2026, 4, 29, 15, 37, 40, 538, DateTimeKind.Utc).AddTicks(1260), "HR", "aliya.bekova@contoso.kz", "Aliya", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bekova", "HR Specialist", 600000m, "OnVacation", null, null },
                    { new Guid("7df96215-6563-403b-97e2-781e7b65b50a"), new DateTime(2026, 4, 29, 15, 37, 40, 538, DateTimeKind.Utc).AddTicks(1250), "Design", "madina.serikova@contoso.kz", "Madina", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Serikova", "Product Designer", 700000m, "Active", null, null },
                    { new Guid("b1cb6b2f-8277-4896-a0bd-72e10d9733ff"), new DateTime(2026, 4, 29, 15, 37, 40, 538, DateTimeKind.Utc).AddTicks(1260), "Engineering", "ruslan.iskakov@contoso.kz", "Ruslan", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Iskakov", "DevOps Engineer", 900000m, "Active", null, null },
                    { new Guid("cbd36a56-9cdd-4911-bb83-0ffe6e6781b8"), new DateTime(2026, 4, 29, 15, 37, 40, 538, DateTimeKind.Utc).AddTicks(1240), "Engineering", "daniyar.akhmetov@contoso.kz", "Daniyar", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Akhmetov", "Engineering Manager", 1200000m, "Active", null, null },
                    { new Guid("e9ef064f-48d9-43da-86b9-47e5dd32f76f"), new DateTime(2026, 4, 29, 15, 37, 40, 538, DateTimeKind.Utc).AddTicks(1240), "Engineering", "aigerim.nurlanova@contoso.kz", "Aigerim", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nurlanova", "Senior Software Engineer", 85000m, "Active", null, null }
                });
        }
    }
}
