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
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    grade = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    termination_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "fk_employees_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_employees_positions_position_id",
                        column: x => x.position_id,
                        principalTable: "positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "salaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_salaries", x => x.id);
                    table.ForeignKey(
                        name: "fk_salaries_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "departments",
                columns: new[] { "id", "created_at", "description", "name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Engineering", null },
                    { new Guid("22222222-0000-0000-0000-000000000002"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Design", null },
                    { new Guid("22222222-0000-0000-0000-000000000003"), new DateTime(2019, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "HR", null }
                });

            migrationBuilder.InsertData(
                table: "positions",
                columns: new[] { "id", "created_at", "description", "grade", "title", "updated_at" },
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
                table: "employees",
                columns: new[] { "id", "created_at", "department_id", "email", "first_name", "hire_date", "last_name", "position_id", "status", "termination_date", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "aigerim.nurlanova@contoso.kz", "Aigerim", new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Nurlanova", new Guid("33333333-0000-0000-0000-000000000001"), "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000002"), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "daniyar.akhmetov@contoso.kz", "Daniyar", new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Akhmetov", new Guid("33333333-0000-0000-0000-000000000002"), "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000003"), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000002"), "madina.serikova@contoso.kz", "Madina", new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Serikova", new Guid("33333333-0000-0000-0000-000000000003"), "Active", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000004"), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "yerlan.tursynov@contoso.kz", "Yerlan", new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Tursynov", new Guid("33333333-0000-0000-0000-000000000004"), "Fired", new DateTime(2024, 11, 30, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("11111111-0000-0000-0000-000000000005"), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000003"), "aliya.bekova@contoso.kz", "Aliya", new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bekova", new Guid("33333333-0000-0000-0000-000000000005"), "OnVacation", null, null },
                    { new Guid("11111111-0000-0000-0000-000000000006"), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("22222222-0000-0000-0000-000000000001"), "ruslan.iskakov@contoso.kz", "Ruslan", new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), "Iskakov", new Guid("33333333-0000-0000-0000-000000000006"), "Active", null, null }
                });

            migrationBuilder.InsertData(
                table: "salaries",
                columns: new[] { "id", "amount", "created_at", "effective_date", "employee_id", "end_date", "type", "updated_at" },
                values: new object[,]
                {
                    { new Guid("44444444-0000-0000-0000-000000000001"), 850000m, new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000001"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000002"), 1200000m, new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000002"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000003"), 700000m, new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000003"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000004"), 550000m, new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000004"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000005"), 600000m, new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000005"), null, "Monthly", null },
                    { new Guid("44444444-0000-0000-0000-000000000006"), 900000m, new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000006"), null, "Monthly", null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_position_id",
                table: "employees",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "ix_salaries_employee_id",
                table: "salaries",
                column: "employee_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "salaries");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "positions");
        }
    }
}
