using AccountingHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasData(
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Aigerim Nurlanova",
                    Email = "aigerim.nurlanova@contoso.kz",
                    Position = "Senior Software Engineer",
                    Salary = 850_00m,
                    Department = "Engineering",
                    HireDate = new DateTime(2021, 3, 15),
                    TerminationDate = null,
                    Status = "Active"
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Daniyar Akhmetov",
                    Email = "daniyar.akhmetov@contoso.kz",
                    Position = "Engineering Manager",
                    Salary = 1_200_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2019, 8, 1),
                    TerminationDate = null,
                    Status = "Active"
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Madina Serikova",
                    Email = "madina.serikova@contoso.kz",
                    Position = "Product Designer",
                    Salary = 700_000m,
                    Department = "Design",
                    HireDate = new DateTime(2022, 1, 10),
                    TerminationDate = null,
                    Status = "Active"
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Yerlan Tursynov",
                    Email = "yerlan.tursynov@contoso.kz",
                    Position = "QA Engineer",
                    Salary = 550_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2020, 5, 20),
                    TerminationDate = new DateTime(2024, 11, 30),
                    Status = "Terminated"
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Aliya Bekova",
                    Email = "aliya.bekova@contoso.kz",
                    Position = "HR Specialist",
                    Salary = 600_000m,
                    Department = "HR",
                    HireDate = new DateTime(2023, 6, 1),
                    TerminationDate = null,
                    Status = "OnLeave"
                },
                new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Ruslan Iskakov",
                    Email = "ruslan.iskakov@contoso.kz",
                    Position = "DevOps Engineer",
                    Salary = 900_000m,
                    Department = "Engineering",
                    HireDate = new DateTime(2021, 11, 4),
                    TerminationDate = null,
                    Status = "Active"
                });
        });
    }
}