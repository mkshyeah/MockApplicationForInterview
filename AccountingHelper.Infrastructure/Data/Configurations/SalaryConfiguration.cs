using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class SalaryConfiguration : IEntityTypeConfiguration<SalaryEntity>
{
    public void Configure(EntityTypeBuilder<SalaryEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Type)
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasData(
            new SalaryEntity { Id = new Guid("44444444-0000-0000-0000-000000000001"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000001"), Amount = 850_000m, Type = SalaryType.Monthly, EffectiveDate = new DateTime(2021, 3, 15), CreatedAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new SalaryEntity { Id = new Guid("44444444-0000-0000-0000-000000000002"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000002"), Amount = 1_200_000m, Type = SalaryType.Monthly, EffectiveDate = new DateTime(2019, 8, 1), CreatedAt = new DateTime(2019, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
            new SalaryEntity { Id = new Guid("44444444-0000-0000-0000-000000000003"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000003"), Amount = 700_000m, Type = SalaryType.Monthly, EffectiveDate = new DateTime(2022, 1, 10), CreatedAt = new DateTime(2022, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new SalaryEntity { Id = new Guid("44444444-0000-0000-0000-000000000004"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000004"), Amount = 550_000m, Type = SalaryType.Monthly, EffectiveDate = new DateTime(2020, 5, 20), CreatedAt = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc) },
            new SalaryEntity { Id = new Guid("44444444-0000-0000-0000-000000000005"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000005"), Amount = 600_000m, Type = SalaryType.Monthly, EffectiveDate = new DateTime(2023, 6, 1), CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new SalaryEntity { Id = new Guid("44444444-0000-0000-0000-000000000006"), EmployeeId = new Guid("11111111-0000-0000-0000-000000000006"), Amount = 900_000m, Type = SalaryType.Monthly, EffectiveDate = new DateTime(2021, 11, 4), CreatedAt = new DateTime(2021, 11, 4, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}