using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.Domain.Services;

public static class EmployeeFactory
{
    public static (Employee Employee, IReadOnlyList<LeaveBalance> Balances) Hire(
        string firstName, string lastName, string email,
        Guid departmentId, Guid positionId, DateTime hireDate,
        decimal salaryAmount, SalaryType salaryType)
    {
        var employeeId = Guid.NewGuid();

        var employee = new Employee
        {
            Id = employeeId,
            FirstName = firstName,
            LastName = lastName,
            Email = EmailNormalizer.Normalize(email),
            DepartmentId = departmentId,
            PositionId = positionId,
            Status = EmployeeStatus.Active,
            HireDate = hireDate,
            Salaries =
            {
                new Salary
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    Amount = salaryAmount,
                    Type = salaryType,
                    EffectiveDate = hireDate
                }
            }
        };

        var balances = Enum.GetValues<LeaveType>()
            .Select(type => new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                LeaveType = type,
                RemainingDays = LeaveEntitlement.DaysFor(type)
            })
            .ToList();

        return (employee, balances);
    }
}