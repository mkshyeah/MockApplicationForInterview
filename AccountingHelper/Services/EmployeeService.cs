using AccountingHelper.Contexts;
using AccountingHelper.Extensions;
using AccountingHelper.Models;
using AccountingHelper.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Services;

public class EmployeeService:IEmployeeService
{
    private readonly ApplicationDbContext _dbContext;

    public EmployeeService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<List<Employee>> GetEmployees(int page , int pageSize , CancellationToken ct )
    {
        return  await _dbContext.Employees
            .AsNoTracking()
            .OrderBy(e => e.CreatedAt)
            .Skip((page -1) * pageSize)
            .Take(pageSize)
            .Select(e => e.ToModel())
            .ToListAsync(ct);
    }

    public async Task<Employee> GetEmployee(Guid id, CancellationToken ct)
    {
        var employeeEntity = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employeeEntity == null)
            throw new KeyNotFoundException($"Employee with id {id} not found");

        return employeeEntity.ToModel();
    }

    public async Task<Employee> CreateEmployee(Employee employee, CancellationToken ct)
    {
        if (employee == null) throw new ArgumentNullException(nameof(employee));
        
        var exists = await _dbContext.Employees.AnyAsync(e => e.Email == employee.Email, ct);
        
        if (exists)
            throw new InvalidOperationException($"Employee with email {employee.Email} already exists");
        
        var employeeEntity = employee.ToEntity();
        employeeEntity.Id = Guid.NewGuid();
        employeeEntity.CreatedAt = DateTime.UtcNow;
        employeeEntity.Status = EmployeeStatus.Active;

        _dbContext.Employees.Add(employeeEntity);
        
        await _dbContext.SaveChangesAsync(ct); 
    
        return employeeEntity.ToModel();
    }

    public async Task<Employee> FireEmployee(Guid id, CancellationToken ct)
    {
        var employeeEntity = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        
        if (employeeEntity == null)
            throw new KeyNotFoundException($"Employee with id {id} not found");

        if (employeeEntity.Status == EmployeeStatus.Fired)
            throw new InvalidOperationException($"Employee with id {id} is fired");
        
        employeeEntity.TerminationDate = DateTime.UtcNow;
        employeeEntity.Status = EmployeeStatus.Fired;
        employeeEntity.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync(ct);

        return employeeEntity.ToModel();
    }
}