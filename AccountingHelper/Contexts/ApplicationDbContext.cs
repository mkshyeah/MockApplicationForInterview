using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.Entity<EmployeeEntity>(e =>
        {
            
            e.Property(entity => entity.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            
            e.Property(entity => entity.Id)
                .HasDefaultValueSql("NEWID()");
            
            e.Property(x => x.Status)
                .HasConversion<string>();
            
            e.Property(x => x.Salary)
                .HasPrecision(18, 2);
            
            e.HasData(
                new EmployeeEntity
                {
                    Id = new Guid("11111111-0000-0000-0000-000000000001"),
                    FirstName = "Aigerim",
                    LastName = "Nurlanova",
                    Email = "aigerim.nurlanova@contoso.kz",
                    Position = "Senior Software Engineer",
                    Salary = 850_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2021, 3, 15),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = new DateTime(2021, 3, 15, 0, 0, 0, DateTimeKind.Utc)
                },
                new EmployeeEntity
                {
                    Id = new Guid("11111111-0000-0000-0000-000000000002"),
                    FirstName = "Daniyar",
                    LastName = "Akhmetov",
                    Email = "daniyar.akhmetov@contoso.kz",
                    Position = "Engineering Manager",
                    Salary = 1_200_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2019, 8, 1),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = new DateTime(2019, 8, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new EmployeeEntity
                {
                    Id = new Guid("11111111-0000-0000-0000-000000000003"),
                    FirstName = "Madina",
                    LastName = "Serikova",
                    Email = "madina.serikova@contoso.kz",
                    Position = "Product Designer",
                    Salary = 700_000m,
                    Department = "Design",
                    HireDate = new DateTime(2022, 1, 10),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = new DateTime(2022, 1, 10, 0, 0, 0, DateTimeKind.Utc)
                },
                new EmployeeEntity
                {
                    Id = new Guid("11111111-0000-0000-0000-000000000004"),
                    FirstName = "Yerlan",
                    LastName = "Tursynov",
                    Email = "yerlan.tursynov@contoso.kz",
                    Position = "QA Engineer",
                    Salary = 550_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2020, 5, 20),
                    TerminationDate = new DateTime(2024, 11, 30),
                    Status = EmployeeStatus.Fired,
                    CreatedAt = new DateTime(2020, 5, 20, 0, 0, 0, DateTimeKind.Utc)
                },
                new EmployeeEntity
                {
                    Id = new Guid("11111111-0000-0000-0000-000000000005"),
                    FirstName = "Aliya",
                    LastName = "Bekova",
                    Email = "aliya.bekova@contoso.kz",
                    Position = "HR Specialist",
                    Salary = 600_000m,
                    Department = "HR",
                    HireDate = new DateTime(2023, 6, 1),
                    TerminationDate = null,
                    Status = EmployeeStatus.OnVacation,
                    CreatedAt = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new EmployeeEntity
                {
                    Id = new Guid("11111111-0000-0000-0000-000000000006"),
                    FirstName = "Ruslan",
                    LastName = "Iskakov",
                    Email = "ruslan.iskakov@contoso.kz",
                    Position = "DevOps Engineer",
                    Salary = 900_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2021, 11, 4),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = new DateTime(2021, 11, 4, 0, 0, 0, DateTimeKind.Utc)
                });
        });
    }
}