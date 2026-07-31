using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalanceEntity>
{
    public void Configure(EntityTypeBuilder<LeaveBalanceEntity> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("ck_leave_balances_remaining_days", "remaining_days >= 0"));
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.LeaveType)
            .HasConversion<string>();
        
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        
        builder.Property<uint>("xmin").IsRowVersion();
        
        builder.HasOne<EmployeeEntity>()
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // один баланс на пару «сотрудник + тип» — схемный инвариант, без него списания разъедутся по строкам
        builder.HasIndex(e => new { e.EmployeeId, e.LeaveType }).IsUnique();

        builder.HasData(
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0000-000000000001"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000001"), LeaveType = LeaveType.Annual, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Annual) - 14, CreatedAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0000-000000000002"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000002"), LeaveType = LeaveType.Annual, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Annual), CreatedAt = new DateTime(2019, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0000-000000000003"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000003"), LeaveType = LeaveType.Annual, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Annual), CreatedAt = new DateTime(2022, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0000-000000000004"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000004"), LeaveType = LeaveType.Annual, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Annual), CreatedAt = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0000-000000000005"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000005"), LeaveType = LeaveType.Annual, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Annual) - 12, CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0000-000000000006"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000006"), LeaveType = LeaveType.Annual, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Annual), CreatedAt = new DateTime(2021, 11, 4, 0, 0, 0, DateTimeKind.Utc) },
        
            // Sick — 14 дней, ни одной одобренной Sick-заявки нет, у всех полный остаток
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0001-000000000001"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000001"), LeaveType = LeaveType.Sick, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Sick), CreatedAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0001-000000000002"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000002"), LeaveType = LeaveType.Sick, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Sick), CreatedAt = new DateTime(2019, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0001-000000000003"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000003"), LeaveType = LeaveType.Sick, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Sick), CreatedAt = new DateTime(2022, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0001-000000000004"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000004"), LeaveType = LeaveType.Sick, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Sick), CreatedAt = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0001-000000000005"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000005"), LeaveType = LeaveType.Sick, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Sick), CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0001-000000000006"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000006"), LeaveType = LeaveType.Sick, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Sick), CreatedAt = new DateTime(2021, 11, 4, 0, 0, 0, DateTimeKind.Utc) },

            // Unpaid — 30 дней, заявка №6 отклонена, баланс не тронут
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0002-000000000001"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000001"), LeaveType = LeaveType.Unpaid, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Unpaid), CreatedAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0002-000000000002"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000002"), LeaveType = LeaveType.Unpaid, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Unpaid), CreatedAt = new DateTime(2019, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0002-000000000003"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000003"), LeaveType = LeaveType.Unpaid, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Unpaid), CreatedAt = new DateTime(2022, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0002-000000000004"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000004"), LeaveType = LeaveType.Unpaid, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Unpaid), CreatedAt = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0002-000000000005"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000005"), LeaveType = LeaveType.Unpaid, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Unpaid), CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new LeaveBalanceEntity { Id = new Guid("66666666-0000-0000-0002-000000000006"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000006"), LeaveType = LeaveType.Unpaid, RemainingDays = LeaveEntitlement.DaysFor(LeaveType.Unpaid), CreatedAt = new DateTime(2021, 11, 4, 0, 0, 0, DateTimeKind.Utc) }
            );
        
    }
}