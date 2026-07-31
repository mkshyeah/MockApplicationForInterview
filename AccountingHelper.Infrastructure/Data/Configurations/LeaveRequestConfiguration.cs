using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequestEntity>
{
    public void Configure(EntityTypeBuilder<LeaveRequestEntity> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.LeaveType)
            .HasConversion<string>();
        
        builder.Property(e => e.Status)
            .HasConversion<string>();
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        // связи
        builder.HasOne(e => e.Employee)
        .WithMany()
        .HasForeignKey(e => e.EmployeeId)
        .IsRequired()
        .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.EmployeeId, e.Status });

        builder.HasData(
            // одобрен: 14 дней Annual → остаток 14
            new LeaveRequestEntity { Id = new Guid("55555555-0000-0000-0000-000000000001"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000001"), LeaveType = LeaveType.Annual, StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 7, 14), Status = LeaveStatus.Approved, RequestedAt = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc), DecidedAt = new DateTime(2024, 6, 12, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequestEntity { Id = new Guid("55555555-0000-0000-0000-000000000002"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000002"), LeaveType = LeaveType.Sick, StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7), Status = LeaveStatus.Pending, RequestedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequestEntity { Id = new Guid("55555555-0000-0000-0000-000000000003"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000003"), LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 8, 10), EndDate = new DateOnly(2026, 8, 20), Status = LeaveStatus.Pending, RequestedAt = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequestEntity { Id = new Guid("55555555-0000-0000-0000-000000000004"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000004"), LeaveType = LeaveType.Annual, StartDate = new DateOnly(2024, 10, 1), EndDate = new DateOnly(2024, 10, 10), Status = LeaveStatus.Rejected, RequestedAt = new DateTime(2024, 9, 15, 0, 0, 0, DateTimeKind.Utc), DecidedAt = new DateTime(2024, 9, 16, 0, 0, 0, DateTimeKind.Utc) },
            // одобрен и идёт прямо сейчас: согласуется с EmployeeStatus.OnVacation, 12 дней → остаток 16
            new LeaveRequestEntity { Id = new Guid("55555555-0000-0000-0000-000000000005"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000005"), LeaveType = LeaveType.Annual, StartDate = new DateOnly(2026, 7, 27), EndDate = new DateOnly(2026, 8, 7), Status = LeaveStatus.Approved, RequestedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), DecidedAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveRequestEntity { Id = new Guid("55555555-0000-0000-0000-000000000006"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000006"), LeaveType = LeaveType.Unpaid, StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 9, 3), Status = LeaveStatus.Rejected, RequestedAt = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc), DecidedAt = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}