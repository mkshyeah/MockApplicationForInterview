using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Salaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Salaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Engineering", null },
                    { new Guid("22222222-0000-0000-0000-000000000002"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Design", null },
                    { new Guid("22222222-0000-0000-0000-000000000003"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HR", null }
                });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "CreatedAt", "Description", "Grade", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("33333333-0000-0000-0000-000000000001"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Senior", "Senior Software Engineer", null },
                    { new Guid("33333333-0000-0000-0000-000000000002"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Lead", "Engineering Manager", null },
                    { new Guid("33333333-0000-0000-0000-000000000003"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Middle", "Product Designer", null },
                    { new Guid("33333333-0000-0000-0000-000000000004"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Middle", "QA Engineer", null },
                    { new Guid("33333333-0000-0000-0000-000000000005"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Junior", "HR Specialist", null },
                    { new Guid("33333333-0000-0000-0000-000000000006"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Senior", "DevOps Engineer", null }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CreatedAt", "DepartmentId", "Email", "FirstName", "HireDate", "LastName", "PositionId", "Status", "TerminationDate", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "aigerim.nurlanova@contoso.kz", "Aigerim", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Nurlanova", new Guid("33333333-0000-0000-0000-000000000001"), "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000002"), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "daniyar.akhmetov@contoso.kz", "Daniyar", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Akhmetov", new Guid("33333333-0000-0000-0000-000000000002"), "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000003"), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000002"), "madina.serikova@contoso.kz", "Madina", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Serikova", new Guid("33333333-0000-0000-0000-000000000003"), "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000004"), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "yerlan.tursynov@contoso.kz", "Yerlan", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tursynov", new Guid("33333333-0000-0000-0000-000000000004"), "Fired", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), null },
                    { new Guid("11111111-0000-0000-0000-000000000005"), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000003"), "aliya.bekova@contoso.kz", "Aliya", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Bekova", new Guid("33333333-0000-0000-0000-000000000005"), "OnVacation", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000006"), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "ruslan.iskakov@contoso.kz", "Ruslan", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Iskakov", new Guid("33333333-0000-0000-0000-000000000006"), "Active", null, null }
                });

            migrationBuilder.InsertData(
                table: "Salaries",
                columns: new[] { "Id", "Amount", "CreatedAt", "EffectiveDate", "EmployeeId", "EndDate", "Type", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000001"), 850000m, new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("11111111-0000-0000-0000-000000000001"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000002"), 1200000m, new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("11111111-0000-0000-0000-000000000002"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000003"), 700000m, new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("11111111-0000-0000-0000-000000000003"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000004"), 550000m, new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("11111111-0000-0000-0000-000000000004"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000005"), 600000m, new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("11111111-0000-0000-0000-000000000005"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000006"), 900000m, new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), new Guid("11111111-0000-0000-0000-000000000006"), null, "Monthly", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_EmployeeId",
                table: "Salaries",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Salaries");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
