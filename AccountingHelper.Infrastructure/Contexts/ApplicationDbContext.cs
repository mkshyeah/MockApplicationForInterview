using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AccountingHelper.Infrastructure.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<EmployeeEntity> Employees => Set<EmployeeEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<SalaryEntity> Salaries => Set<SalaryEntity>();
    
    public DbSet<LeaveRequestEntity> LeaveRequests => Set<LeaveRequestEntity>();
    public DbSet<LeaveBalanceEntity> LeaveBalances => Set<LeaveBalanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            entityType.FindProperty("CreatedAt")?.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
    }
}