using AccountingHelper.Application.DTOs.Requests;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Application.Interfaces;

public interface IEmployeeService
{

    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetEmployees(EmployeeFilteredRequest request, CancellationToken ct);

    Task<Employee> GetEmployee(Guid id,CancellationToken ct);
    
}