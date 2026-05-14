using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountingHelper.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWID()");

        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // связи
        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Salaries)
            .WithOne(s => s.Employee)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(
            new EmployeeEntity { Id = new Guid("11111111-0000-0000-0000-000000000001"), FirstName = "Aigerim", LastName = "Nurlanova", Email = "aigerim.nurlanova@contoso.kz", PositionId = new Guid("33333333-0000-0000-0000-000000000001"), DepartmentId = new Guid("22222222-0000-0000-0000-000000000001"), HireDate = new DateTime(2021, 3, 15), Status = EmployeeStatus.Active, CreatedAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new EmployeeEntity { Id = new Guid("11111111-0000-0000-0000-000000000002"), FirstName = "Daniyar", LastName = "Akhmetov", Email = "daniyar.akhmetov@contoso.kz", PositionId = new Guid("33333333-0000-0000-0000-000000000002"), DepartmentId = new Guid("22222222-0000-0000-0000-000000000001"), HireDate = new DateTime(2019, 8, 1), Status = EmployeeStatus.Active, CreatedAt = new DateTime(2019, 8, 1, 0, 0, 0, DateTimeKind.Utc) },
            new EmployeeEntity { Id = new Guid("11111111-0000-0000-0000-000000000003"), FirstName = "Madina", LastName = "Serikova", Email = "madina.serikova@contoso.kz", PositionId = new Guid("33333333-0000-0000-0000-000000000003"), DepartmentId = new Guid("22222222-0000-0000-0000-000000000002"), HireDate = new DateTime(2022, 1, 10), Status = EmployeeStatus.Active, CreatedAt = new DateTime(2022, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
            new EmployeeEntity { Id = new Guid("11111111-0000-0000-0000-000000000004"), FirstName = "Yerlan", LastName = "Tursynov", Email = "yerlan.tursynov@contoso.kz", PositionId = new Guid("33333333-0000-0000-0000-000000000004"), DepartmentId = new Guid("22222222-0000-0000-0000-000000000001"), HireDate = new DateTime(2020, 5, 20), TerminationDate = new DateTime(2024, 11, 30), Status = EmployeeStatus.Fired, CreatedAt = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc) },
            new EmployeeEntity { Id = new Guid("11111111-0000-0000-0000-000000000005"), FirstName = "Aliya", LastName = "Bekova", Email = "aliya.bekova@contoso.kz", PositionId = new Guid("33333333-0000-0000-0000-000000000005"), DepartmentId = new Guid("22222222-0000-0000-0000-000000000003"), HireDate = new DateTime(2023, 6, 1), Status = EmployeeStatus.OnVacation, CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
            new EmployeeEntity { Id = new Guid("11111111-0000-0000-0000-000000000006"), FirstName = "Ruslan", LastName = "Iskakov", Email = "ruslan.iskakov@contoso.kz", PositionId = new Guid("33333333-0000-0000-0000-000000000006"), DepartmentId = new Guid("22222222-0000-0000-0000-000000000001"), HireDate = new DateTime(2021, 11, 4), Status = EmployeeStatus.Active, CreatedAt = new DateTime(2021, 11, 4, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}