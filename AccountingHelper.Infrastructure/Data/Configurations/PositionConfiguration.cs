using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<PositionEntity>
{
    public void Configure(EntityTypeBuilder<PositionEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Grade)
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasData(
            new PositionEntity { Id = new Guid("33333333-0000-0000-0000-000000000001"), Title = "Senior Software Engineer", Grade = EmployeeGrade.Senior, CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PositionEntity { Id = new Guid("33333333-0000-0000-0000-000000000002"), Title = "Engineering Manager", Grade = EmployeeGrade.Lead, CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PositionEntity { Id = new Guid("33333333-0000-0000-0000-000000000003"), Title = "Product Designer", Grade = EmployeeGrade.Middle, CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PositionEntity { Id = new Guid("33333333-0000-0000-0000-000000000004"), Title = "QA Engineer", Grade = EmployeeGrade.Middle, CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PositionEntity { Id = new Guid("33333333-0000-0000-0000-000000000005"), Title = "HR Specialist", Grade = EmployeeGrade.Junior, CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new PositionEntity { Id = new Guid("33333333-0000-0000-0000-000000000006"), Title = "DevOps Engineer", Grade = EmployeeGrade.Senior, CreatedAt = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}