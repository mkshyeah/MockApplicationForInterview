using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

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
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<ITimestamped>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }
}