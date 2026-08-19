using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ReportingTests : IntegrationTestBase
{
    public ReportingTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetEmployeeCount_ReturnsTotalNumberOfEmployees()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        for (int i = 0; i < 6; i++)
        {
            await CreateEmployeeAsync(seedDepartmentId, seedPositionId);
        }
        
        // ACT
        var resp2 = await Client.GetAsync("v1/reporting/employees/count");
        
        // ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await resp2.Content.ReadFromJsonAsync<int>(Json);
        count.Should().Be(6);
    }

    [Fact]
    public async Task GetActiveSalarySum_ExcludesClosedSalaryOfFiredEmployee()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
 
        var employeeIds = new List<Guid>();
        for (var i = 1; i < 6; i++)
        {
            var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 1000m * i);
            employeeIds.Add(body.Id);
        }
 
        // увольняем Emp3 (зарплата 3000) — его закрытая зарплата не должна попасть в сумму
        var fireResp = await Client.PatchAsync($"v1/employees/{employeeIds[2]}/fire", content: null);
        fireResp.StatusCode.Should().Be(HttpStatusCode.OK);
 
        // ACT
        var resp2 = await Client.GetAsync("v1/reporting/salaries");
 
        // ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp2.Content.ReadFromJsonAsync<decimal>(Json);
        amount.Should().Be(12000m); // 15000 - 3000 уволенного
    }
    
    [Fact]
    public async Task GetActiveSalarySum_ReflectsRaisedEmployeeSalary()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
 
        var employeeIds = new List<Guid>();
        for (var i = 1; i < 6; i++)
        {
            var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 1000m * i);
            employeeIds.Add(body.Id);
        }
 
        // поднимаем зарплату Emp1 с 1000 до 10000 — старая должна закрыться, новая стать активной
        var changeResp = await Client.PutAsJsonAsync($"v1/employees/{employeeIds[0]}/salaries", new
        {
            amount = 10000m, salaryType = "Monthly"
        });
        changeResp.StatusCode.Should().Be(HttpStatusCode.OK);
 
        // ACT
        var resp2 = await Client.GetAsync("v1/reporting/salaries");
 
        // ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp2.Content.ReadFromJsonAsync<decimal>(Json);
        amount.Should().Be(24000m); // 15000 - 1000 (старая Emp1) + 10000 (новая Emp1)
    }

    [Fact]
    public async Task GetEmployeeTaxes_ReturnsMonthlyTaxForActiveSalary()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);
        body.Should().NotBeNull();

        var changeResp = await Client.PutAsJsonAsync($"v1/employees/{body.Id}/salaries", new
        {
            amount = 900000m, salaryType = "Monthly"
        });
        changeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT
        var resp2 = await Client.GetAsync($"v1/reporting/employees/{body.Id}/taxes");
        
        // ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp2.Content.ReadFromJsonAsync<decimal>(Json);
        
        amount.Should().Be(270000m);
    }
    
    [Theory]
    [InlineData("600000", "60000")]         // ровно граница low/mid → остаётся LowTaxRate (10%)
    [InlineData("600000.01", "120000.002")] // на копейку выше → вся сумма уже по MidTaxRate (20%)
    [InlineData("800000", "160000")]        // ровно граница mid/high → остаётся MidTaxRate (20%)
    [InlineData("800000.01", "240000.003")] // на копейку выше → вся сумма уже по HighTaxRate (30%)
    public async Task GetEmployeeTaxes_AtBracketBoundaries_UsesCorrectRate(string salaryStr, string expectedTaxStr)
    {
        var salary = decimal.Parse(salaryStr, CultureInfo.InvariantCulture);
        var expectedTax = decimal.Parse(expectedTaxStr, CultureInfo.InvariantCulture);
 
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: salary);

        // ACT
        var resp2 = await Client.GetAsync($"v1/reporting/employees/{body.Id}/taxes");
 
        // ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp2.Content.ReadFromJsonAsync<decimal>(Json);
 
        amount.Should().Be(expectedTax);
    }
    
    [Fact]
    public async Task GetEmployeeStatus_WhenNotFound_Returns404()
    {
        // ARRANGE
        var missingEmployeeId = Guid.NewGuid();

        // ACT
        var resp = await Client.GetAsync($"v1/reporting/employees/{missingEmployeeId}/status");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActiveSalarySum_ConvertsNonMonthlySalariesToMonthly()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 5000m);
        // 60/hour -> 60 * 2080 / 12 = 10 000 per month (exact, no rounding involved)
        await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 60m, salaryType: "Hourly");

        // ACT
        var resp = await Client.GetAsync("v1/reporting/salaries");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp.Content.ReadFromJsonAsync<decimal>(Json);

        // summing raw amounts would give 5060 — a number that means nothing,
        // since it adds an hourly rate to a monthly one
        amount.Should().Be(15_400m);
    }

    [Fact]
    public async Task GetActiveSalarySum_ExcludesFiredEmployeeWhoseSalaryWasNeverClosed()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 5000m);
        var fired = await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 3000m);

        // bypass the fire endpoint on purpose: it closes the salary, and this test is about
        // the data state where that invariant was broken (bulk import, migration, manual fix).
        // EndDate == null alone would keep the employee in payroll forever.
        await WithDbContextAsync(async db =>
        {
            var entity = await db.Employees.SingleAsync(e => e.Id == fired.Id);
            entity.Status = EmployeeStatus.Fired;
            await db.SaveChangesAsync();
        });

        // ACT
        var resp = await Client.GetAsync("v1/reporting/salaries");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp.Content.ReadFromJsonAsync<decimal>(Json);

        amount.Should().Be(5000m);
    }

    [Fact]
    public async Task GetActiveSalarySum_IncludesEmployeeOnVacation()
    {
        // ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId, salary: 5000m);

        var vacationResp = await Client.PatchAsync($"v1/employees/{employee.Id}/on-vacation", content: null);
        vacationResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT
        var resp = await Client.GetAsync("v1/reporting/salaries");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var amount = await resp.Content.ReadFromJsonAsync<decimal>(Json);

        // people on vacation are still paid: filtering on Status == Active instead of
        // Status != Fired would silently drop them from payroll
        amount.Should().Be(5000m);
    }
}