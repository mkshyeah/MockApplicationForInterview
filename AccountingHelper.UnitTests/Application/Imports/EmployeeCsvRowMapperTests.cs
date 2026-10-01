using AccountingHelper.Application.Features.Imports.Parsing;
using AccountingHelper.Domain.Enums;
using FluentAssertions;

namespace AccountingHelper.UnitTests.Application.Imports;

public class EmployeeCsvRowMapperTests
{
    private static readonly Guid EngineeringId = Guid.NewGuid();
    private static readonly Guid DeveloperId = Guid.NewGuid();

    // Built the way the run handler builds them: exact, case-sensitive names, like the unique index.
    private static readonly IReadOnlyDictionary<string, Guid> Departments =
        new Dictionary<string, Guid>(StringComparer.Ordinal) { ["Engineering"] = EngineeringId };

    private static readonly IReadOnlyDictionary<string, Guid> Positions =
        new Dictionary<string, Guid>(StringComparer.Ordinal) { ["Developer"] = DeveloperId };

    private static EmployeeCsvRow Row(
        string salaryAmount = "1000.50",
        string salaryType = "Monthly",
        string hireDate = "2024-01-15",
        string department = "Engineering",
        string position = "Developer") => new()
    {
        FirstName = "Aigerim",
        LastName = "Nurlanova",
        Email = "a@test.com",
        Department = department,
        Position = position,
        SalaryAmount = salaryAmount,
        SalaryType = salaryType,
        HireDate = hireDate
    };

    private static CsvRowMapping Map(EmployeeCsvRow row) => EmployeeCsvRowMapper.Map(row, Departments, Positions);

    [Fact]
    public void Map_WithValidRow_BuildsCommand()
    {
        // ACT
        var result = Map(Row());

        // ASSERT
        result.Errors.Should().BeEmpty();

        var command = result.Command!;
        command.FirstName.Should().Be("Aigerim");
        command.LastName.Should().Be("Nurlanova");
        command.Email.Should().Be("a@test.com");
        command.SalaryAmount.Should().Be(1000.50m);
        command.SalaryType.Should().Be(SalaryType.Monthly);
        command.DepartmentId.Should().Be(EngineeringId);
        command.PositionId.Should().Be(DeveloperId);
        command.HireDate.Should().Be(new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("2024-01-15")]
    [InlineData("15.01.2024")]
    public void Map_AcceptsBothContractDateFormats_AsUtc(string hireDate)
    {
        // ACT
        var command = Map(Row(hireDate: hireDate)).Command!;

        // ASSERT
        command.HireDate.Should().Be(new DateTime(2024, 1, 15));
        // hire_date is timestamptz, and Npgsql rejects a DateTime whose Kind is not Utc.
        command.HireDate.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Map_ReadsDottedDateDayFirst()
    {
        // ACT
        var command = Map(Row(hireDate: "01.02.2024")).Command!;

        // ASSERT
        // The ambiguity the contract settles: 1 February, never 2 January.
        command.HireDate.Month.Should().Be(2);
        command.HireDate.Day.Should().Be(1);
    }

    [Theory]
    [InlineData("01/15/2024")]
    [InlineData("15/01/2024")]
    [InlineData("2024.01.15")]
    [InlineData("15.1.2024")]
    [InlineData("31.02.2024")]
    [InlineData("")]
    public void Map_WithDateOutsideContract_ReportsError(string hireDate)
    {
        // ACT
        var result = Map(Row(hireDate: hireDate));

        // ASSERT
        result.Command.Should().BeNull();
        result.Errors.Should().ContainSingle().Which.Should().Contain("HireDate");
    }

    [Theory]
    [InlineData("1000", 1000)]
    [InlineData("1000.5", 1000.5)]
    [InlineData("-5", -5)]
    public void Map_ParsesAmountWithDotDecimalSeparator(string salaryAmount, decimal expected)
    {
        // ACT
        var command = Map(Row(salaryAmount: salaryAmount)).Command!;

        // ASSERT
        // A negative amount passes here on purpose: the "greater than zero" rule belongs to the validator.
        command.SalaryAmount.Should().Be(expected);
    }

    [Theory]
    [InlineData("1000,50")]
    [InlineData("1,000")]
    [InlineData("abc")]
    [InlineData("")]
    public void Map_WithAmountThatIsNotDotDecimal_ReportsError(string salaryAmount)
    {
        // ACT
        var result = Map(Row(salaryAmount: salaryAmount));

        // ASSERT
        // "1000,50" is the case that matters: with thousands separators allowed it would silently become 100050.
        result.Command.Should().BeNull();
        result.Errors.Should().ContainSingle().Which.Should().Contain("SalaryAmount");
    }

    [Theory]
    [InlineData("monthly", SalaryType.Monthly)]
    [InlineData("HOURLY", SalaryType.Hourly)]
    [InlineData("Daily", SalaryType.Daily)]
    public void Map_ParsesSalaryTypeByNameIgnoringCase(string salaryType, SalaryType expected)
    {
        Map(Row(salaryType: salaryType)).Command!.SalaryType.Should().Be(expected);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("Yearly")]
    [InlineData("")]
    public void Map_WithSalaryTypeThatIsNotAName_ReportsError(string salaryType)
    {
        // ACT
        var result = Map(Row(salaryType: salaryType));

        // ASSERT
        // "2" is a valid enum value, but only the names are part of the file contract.
        result.Command.Should().BeNull();
        result.Errors.Should().ContainSingle().Which.Should().Contain("SalaryType");
    }

    [Fact]
    public void Map_WithDepartmentInDifferentCase_ReportsUnknownDepartment()
    {
        // ACT
        var result = Map(Row(department: "engineering"));

        // ASSERT
        // Case-sensitive like the unique index: a case-insensitive lookup over a case-sensitive index is the
        // same split that ruled out citext for email.
        result.Errors.Should().ContainSingle().Which.Should().Be("Unknown department 'engineering'.");
    }

    [Fact]
    public void Map_WithUnknownPosition_ReportsError()
    {
        Map(Row(position: "Astronaut")).Errors.Should().ContainSingle()
            .Which.Should().Be("Unknown position 'Astronaut'.");
    }

    [Fact]
    public void Map_WithSeveralBadFields_ReportsEveryConversionError()
    {
        // ACT
        var result = Map(Row(salaryAmount: "x", salaryType: "x", hireDate: "x", department: "x", position: "x"));

        // ASSERT
        result.Command.Should().BeNull();
        result.Errors.Should().HaveCount(5);
    }

    [Fact]
    public void Map_WithMissingTextFields_LeavesThemEmptyForTheValidator()
    {
        // ARRANGE
        var row = Row();
        row.FirstName = null;
        row.Email = null;

        // ACT
        var result = Map(row);

        // ASSERT
        // Empty names and emails are business rules, reported by CreateEmployeeCommandValidator, not here.
        result.Errors.Should().BeEmpty();
        result.Command!.FirstName.Should().BeEmpty();
        result.Command.Email.Should().BeEmpty();
    }
}
