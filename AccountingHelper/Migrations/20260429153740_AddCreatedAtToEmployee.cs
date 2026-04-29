using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingHelper.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: "0f48f0d8-00bc-4ed4-805b-4c41efdb2f42");

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: "60a5eae8-a941-410f-b215-650222262f55");

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: "658fddf0-11f6-4c59-8622-3fa8af588e0b");

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: "66a5203d-519d-4a64-9aa2-44dd55fa0c5a");

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: "8aaeff3d-e216-49d7-9e03-8f46a3f82a7f");

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: "a93249a9-842f-4b55-b594-f49711dfb7fd");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Employees",
                newName: "LastName");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Employees",
                newName: "Name");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

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
    }
}
