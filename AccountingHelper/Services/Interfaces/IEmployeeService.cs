using AccountingHelper.Domain.Models;
using ErrorOr;

namespace AccountingHelper.Services.Interfaces;

public interface IEmployeeService
{

    Task<ErrorOr<List<Employee>>> GetEmployees(int page , int pageSize, CancellationToken ct );

    Task<ErrorOr<Employee>> GetEmployee(Guid id,CancellationToken ct);

    Task<ErrorOr<Employee>>CreateEmployee(Employee employee,CancellationToken ct);

    Task<ErrorOr<Employee>> FireEmployee(Guid id,CancellationToken ct);
}