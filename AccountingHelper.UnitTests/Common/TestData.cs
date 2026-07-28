using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;

namespace AccountingHelper.UnitTests.Common;

public static class TestData
{
    public static Position ValidPosition(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Title = "Developer",
        Grade = EmployeeGrade.Middle
    };
    
    public static Department ValidDepartment(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "HR"
    };
    
    public static Employee ValidEmployee(
        Guid? id = null,
        EmployeeStatus status = EmployeeStatus.Active) => new()
    {
        Id = id ?? Guid.NewGuid(),
        FirstName = "John",
        LastName = "Doe",
        Email = "john@mail.com",
        PositionId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        HireDate = new DateTime(2024, 1, 15),
        Status = status
    };

    public static Salary ActiveSalary(Guid employeeId, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        EmployeeId = employeeId,
        Amount = 1000m,
        Type = SalaryType.Monthly,
        EffectiveDate = new DateTime(2024, 1, 15)
    };
}