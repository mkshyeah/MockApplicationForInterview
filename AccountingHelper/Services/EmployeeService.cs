using AccountingHelper.Contexts;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using AccountingHelper.Extensions;
using AccountingHelper.Services.Interfaces;
using ErrorOr;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Services;

public class EmployeeService:IEmployeeService
{
    private readonly ApplicationDbContext _dbContext;

    public EmployeeService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<ErrorOr<List<Employee>>> GetEmployees(int page , int pageSize , CancellationToken ct )
    {
        if (page < 1)
            return Error.Validation("Page.Invalid", "Номер страницы должен быть больше 0");

        if (pageSize < 1)   
            return Error.Validation("PageSize.Invalid", "Кол-во элементов должно быть больше 0");
        
        if (pageSize > 100)
            return Error.Validation("PageSize.TooLarge", "Нельзя запрашивать более 100 элементов за раз");

        return  await _dbContext.Employees
            .AsNoTracking()
            .OrderBy(e => e.CreatedAt)
            .Skip((page -1) * pageSize)
            .Take(pageSize)
            .Select(e => e.ToModel())
            .ToListAsync(ct);
    }

    public async Task<ErrorOr<Employee>> GetEmployee(Guid id, CancellationToken ct)
    {
        var employeeEntity = await _dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (employeeEntity == null)
            return Error.NotFound("Employee.NotFound",$"Employee with id {id} not found");

        return employeeEntity.ToModel();
    }

    public async Task<ErrorOr<Employee>> CreateEmployee(Employee employee, CancellationToken ct)
    {
        if (employee == null) return Error.Validation();
        
        var exists = await _dbContext.Employees.AnyAsync(e => e.Email == employee.Email, ct);
        
        if (exists)
            return Error.Conflict("Employee.Exist",$"Employee with email {employee.Email} already exists");
        
        var employeeEntity = employee.ToEntity();
        employeeEntity.Status = EmployeeStatus.Active;

        _dbContext.Employees.Add(employeeEntity);
        
        await _dbContext.SaveChangesAsync(ct); 
    
        return employeeEntity.ToModel();
    }

    public async Task<ErrorOr<Employee>> FireEmployee(Guid id, CancellationToken ct)
    {
        var employeeEntity = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        
        if (employeeEntity == null)
            return Error.NotFound("Employee.NotFound",$"Employee with id {id} not found");

        if (employeeEntity.Status == EmployeeStatus.Fired)
            return Error.Conflict("Employee.Fired",$"Employee with id {id} is fired");
        
        employeeEntity.TerminationDate = DateTime.UtcNow;
        employeeEntity.Status = EmployeeStatus.Fired;
        employeeEntity.UpdatedAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync(ct);

        return employeeEntity.ToModel();
    }
}