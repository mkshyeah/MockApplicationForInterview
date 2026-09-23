using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using AccountingHelper.Domain.Services;
using FluentAssertions;

namespace AccountingHelper.UnitTests.Domain.Services;

public class EmployeeFactoryTests
{
    private static readonly Guid DepartmentId = Guid.NewGuid();
    private static readonly Guid PositionId = Guid.NewGuid();
    private static readonly DateTime HireDate = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    private static (Employee Employee, IReadOnlyList<LeaveBalance> Balances) Hire(
        string email = "john@mail.com",
        decimal salaryAmount = 1000m,
        SalaryType salaryType = SalaryType.Monthly) =>
        EmployeeFactory.Hire(
            "John", "Doe", email,
            DepartmentId, PositionId, HireDate,
            salaryAmount, salaryType);

    [Fact]
    public void Hire_CreatesActiveEmployee_WithGivenFields()
    {
        // ACT
        var (employee, _) = Hire();

        // ASSERT
        employee.Id.Should().NotBeEmpty();
        employee.FirstName.Should().Be("John");
        employee.LastName.Should().Be("Doe");
        employee.DepartmentId.Should().Be(DepartmentId);
        employee.PositionId.Should().Be(PositionId);
        employee.HireDate.Should().Be(HireDate);
        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.TerminationDate.Should().BeNull();

        // Ids only: the import must not have to load reference entities per row to hire someone.
        employee.Department.Should().BeNull();
        employee.Position.Should().BeNull();
    }

    [Fact]
    public void Hire_NormalizesEmail()
    {
        // ACT
        var (employee, _) = Hire(email: "  John.Doe@Mail.COM ");

        // ASSERT
        // Normalizing inside the factory is what keeps a caller from storing an email the unique index
        // and the import's own dedup would disagree about.
        employee.Email.Should().Be(EmailNormalizer.Normalize("  John.Doe@Mail.COM "));
        employee.Email.Should().Be("john.doe@mail.com");
    }

    [Fact]
    public void Hire_OpensOneActiveSalary_FromHireDate()
    {
        // ACT
        var (employee, _) = Hire(salaryAmount: 2500m, salaryType: SalaryType.Hourly);

        // ASSERT
        var salary = employee.Salaries.Should().ContainSingle().Subject;
        salary.Id.Should().NotBeEmpty();
        salary.EmployeeId.Should().Be(employee.Id);
        salary.Amount.Should().Be(2500m);
        salary.Type.Should().Be(SalaryType.Hourly);
        salary.EffectiveDate.Should().Be(HireDate);
        salary.EndDate.Should().BeNull();
    }

    [Fact]
    public void Hire_CreatesOneBalancePerLeaveType_WithFullEntitlement()
    {
        // ACT
        var (employee, balances) = Hire();

        // ASSERT
        // Driven by the enum rather than a hardcoded count, so a new leave type fails here until it is entitled.
        balances.Select(b => b.LeaveType).Should().BeEquivalentTo(Enum.GetValues<LeaveType>());

        balances.Should().AllSatisfy(b =>
        {
            b.Id.Should().NotBeEmpty();
            b.EmployeeId.Should().Be(employee.Id);
            b.RemainingDays.Should().Be(LeaveEntitlement.DaysFor(b.LeaveType));
        });

        balances.Select(b => b.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Hire_CalledTwice_ProducesIndependentIds()
    {
        // ACT
        var (first, firstBalances) = Hire();
        var (second, secondBalances) = Hire();

        // ASSERT
        // A batch hires many people in one save; any shared id would collide on the primary key.
        second.Id.Should().NotBe(first.Id);
        second.Salaries.Single().Id.Should().NotBe(first.Salaries.Single().Id);
        secondBalances.Select(b => b.Id).Should().NotIntersectWith(firstBalances.Select(b => b.Id));
    }
}
