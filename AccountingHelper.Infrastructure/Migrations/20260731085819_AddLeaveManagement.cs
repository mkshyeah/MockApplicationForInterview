using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AccountingHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "leave_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type = table.Column<string>(type: "text", nullable: false),
                    remaining_days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_balances", x => x.id);
                    table.CheckConstraint("ck_leave_balances_remaining_days", "remaining_days >= 0");
                    table.ForeignKey(
                        name: "fk_leave_balances_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leave_type = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leave_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_leave_requests_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "leave_balances",
                columns: new[] { "id", "created_at", "employee_id", "leave_type", "remaining_days", "updated_at" },
                values: new object[,]
                {
                    { new Guid("66666666-0000-0000-0000-000000000001"), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000001"), "Annual", 14, null },
                    { new Guid("66666666-0000-0000-0000-000000000002"), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000002"), "Annual", 28, null },
                    { new Guid("66666666-0000-0000-0000-000000000003"), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000003"), "Annual", 28, null },
                    { new Guid("66666666-0000-0000-0000-000000000004"), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000004"), "Annual", 28, null },
                    { new Guid("66666666-0000-0000-0000-000000000005"), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000005"), "Annual", 16, null },
                    { new Guid("66666666-0000-0000-0000-000000000006"), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000006"), "Annual", 28, null },
                    { new Guid("66666666-0000-0000-0001-000000000001"), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000001"), "Sick", 14, null },
                    { new Guid("66666666-0000-0000-0001-000000000002"), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000002"), "Sick", 14, null },
                    { new Guid("66666666-0000-0000-0001-000000000003"), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000003"), "Sick", 14, null },
                    { new Guid("66666666-0000-0000-0001-000000000004"), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000004"), "Sick", 14, null },
                    { new Guid("66666666-0000-0000-0001-000000000005"), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000005"), "Sick", 14, null },
                    { new Guid("66666666-0000-0000-0001-000000000006"), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000006"), "Sick", 14, null },
                    { new Guid("66666666-0000-0000-0002-000000000001"), new DateTime(2021, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000001"), "Unpaid", 30, null },
                    { new Guid("66666666-0000-0000-0002-000000000002"), new DateTime(2019, 8, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000002"), "Unpaid", 30, null },
                    { new Guid("66666666-0000-0000-0002-000000000003"), new DateTime(2022, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000003"), "Unpaid", 30, null },
                    { new Guid("66666666-0000-0000-0002-000000000004"), new DateTime(2020, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000004"), "Unpaid", 30, null },
                    { new Guid("66666666-0000-0000-0002-000000000005"), new DateTime(2023, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000005"), "Unpaid", 30, null },
                    { new Guid("66666666-0000-0000-0002-000000000006"), new DateTime(2021, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("11111111-0000-0000-0000-000000000006"), "Unpaid", 30, null }
                });

            migrationBuilder.InsertData(
                table: "leave_requests",
                columns: new[] { "id", "decided_at", "decided_by", "employee_id", "end_date", "leave_type", "requested_at", "start_date", "status", "updated_at" },
                values: new object[,]
                {
                    { new Guid("55555555-0000-0000-0000-000000000001"), new DateTime(2024, 6, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-0000-0000-0000-000000000001"), new DateOnly(2024, 7, 14), "Annual", new DateTime(2024, 6, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2024, 7, 1), "Approved", null },
                    { new Guid("55555555-0000-0000-0000-000000000002"), null, null, new Guid("11111111-0000-0000-0000-000000000002"), new DateOnly(2026, 8, 7), "Sick", new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 8, 3), "Pending", null },
                    { new Guid("55555555-0000-0000-0000-000000000003"), null, null, new Guid("11111111-0000-0000-0000-000000000003"), new DateOnly(2026, 8, 20), "Annual", new DateTime(2026, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 8, 10), "Pending", null },
                    { new Guid("55555555-0000-0000-0000-000000000004"), new DateTime(2024, 9, 16, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-0000-0000-0000-000000000004"), new DateOnly(2024, 10, 10), "Annual", new DateTime(2024, 9, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2024, 10, 1), "Rejected", null },
                    { new Guid("55555555-0000-0000-0000-000000000005"), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-0000-0000-0000-000000000005"), new DateOnly(2026, 8, 7), "Annual", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 7, 27), "Approved", null },
                    { new Guid("55555555-0000-0000-0000-000000000006"), new DateTime(2026, 8, 16, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-0000-0000-0000-000000000006"), new DateOnly(2026, 9, 3), "Unpaid", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(2026, 9, 1), "Rejected", null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_leave_balances_employee_id_leave_type",
                table: "leave_balances",
                columns: new[] { "employee_id", "leave_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_leave_requests_employee_id_status",
                table: "leave_requests",
                columns: new[] { "employee_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leave_balances");

            migrationBuilder.DropTable(
                name: "leave_requests");
        }
    }
}
