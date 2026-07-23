using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Interfaces;

public interface IEmployeeService
{

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetEmployees(EmployeeFilteredRequest request, CancellationToken ct);

    Task<Employee> GetEmployee(Guid id,CancellationToken ct);

    Task<Employee>CreateEmployee(Employee employee,CancellationToken ct);

    Task<Employee> FireEmployee(Guid id,CancellationToken ct);
    
    Task<Employee> SendOnVacation(Guid id, CancellationToken ct);
    
    Task<Employee> SendOffVacation(Guid id, CancellationToken ct);
    
}