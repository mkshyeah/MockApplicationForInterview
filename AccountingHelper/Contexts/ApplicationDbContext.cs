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
            e.Property(x => x.Status)
                .HasConversion<string>();
            
            e.Property(x => x.Salary)
                .HasPrecision(18, 2);
            
            e.HasData(
                new EmployeeEntity
                {
                    Id = Guid.NewGuid(), 
                    FirstName = "Aigerim",
                    LastName = "Nurlanova",
                    Email = "aigerim.nurlanova@contoso.kz",
                    Position = "Senior Software Engineer",
                    Salary = 850_00m,
                    Department = "Engineering",
                    HireDate = new DateTime(2021, 3, 15),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new EmployeeEntity
                {
                    Id = Guid.NewGuid(), 
                    FirstName = "Daniyar",
                    LastName = "Akhmetov",
                    Email = "daniyar.akhmetov@contoso.kz",
                    Position = "Engineering Manager",
                    Salary = 1_200_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2019, 8, 1),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new EmployeeEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Madina",
                    LastName = "Serikova",
                    Email = "madina.serikova@contoso.kz",
                    Position = "Product Designer",
                    Salary = 700_000m,
                    Department = "Design",
                    HireDate = new DateTime(2022, 1, 10),
                    TerminationDate = null,
                    Status =  EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                },
                new EmployeeEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Yerlan",
                    LastName = "Tursynov",
                    Email = "yerlan.tursynov@contoso.kz",
                    Position = "QA Engineer",
                    Salary = 550_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2020, 5, 20),
                    TerminationDate = new DateTime(2024, 11, 30),
                    Status = EmployeeStatus.Fired,
                    CreatedAt = DateTime.UtcNow
                },
                new EmployeeEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Aliya",
                    LastName = "Bekova",
                    Email = "aliya.bekova@contoso.kz",
                    Position = "HR Specialist",
                    Salary = 600_000m,
                    Department = "HR",
                    HireDate = new DateTime(2023, 6, 1),
                    TerminationDate = null,
                    Status = EmployeeStatus.OnVacation,
                    CreatedAt = DateTime.UtcNow
                },
                new EmployeeEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Ruslan",
                    LastName = "Iskakov",
                    Email = "ruslan.iskakov@contoso.kz",
                    Position = "DevOps Engineer",
                    Salary = 900_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2021, 11, 4),
                    TerminationDate = null,
                    Status = EmployeeStatus.Active,
                    CreatedAt = DateTime.UtcNow
                });
        });
    }
}