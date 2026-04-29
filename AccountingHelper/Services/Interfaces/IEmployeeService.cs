using AccountingHelper.Domain.Models;

namespace AccountingHelper.Services.Interfaces;

public interface IEmployeeService
{

    Task<List<Employee>> GetEmployees(int page , int pageSize, CancellationToken ct );

    Task<Employee> GetEmployee(Guid id,CancellationToken ct);

    Task<Employee> CreateEmployee(Employee employee,CancellationToken ct);

    Task<Employee> FireEmployee(Guid id,CancellationToken ct);
}