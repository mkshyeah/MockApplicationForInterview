using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Infrastructure.Data.Entities;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

[Collection("Integration")]
public class SalaryChangeTests : IntegrationTestBase
{
    public SalaryChangeTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ChangeSalary_ClosesOldSalary_AndOpensExactlyOneActive()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var created = await Client.PostAsJsonAsync("/v1/employees", new
        {
            firstName = "Ann", lastName = "Lee", email = "ann@acme.io",
            positionId = seedPositionId, departmentId = seedDepartmentId,
            salary = 1000m, salaryType = "Monthly", hireDate = "2026-01-01T00:00:00Z"
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var employee = await created.Content.ReadFromJsonAsync<EmployeeResponse>(Json);
        
        //ACT
        var resp = await Client.PutAsJsonAsync(
            $"v1/employees/{employee!.Id}/salaries",
            new {amount = 1500m, salaryType = "Monthly"});
        
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newSalary = await resp.Content.ReadFromJsonAsync<SalaryResponse>(Json);
        newSalary!.Amount.Should().Be(1500m);
        newSalary!.EndDate.Should().BeNull();
        
        var rows = await WithDbContextAsync(db => db.Set<SalaryEntity>()
            .Where(x => x.EmployeeId == employee.Id)
            .OrderBy(s => s.EffectiveDate)
            .ToListAsync());
        
        rows.Count.Should().Be(2);
        rows.Count(s => s.EndDate == null).Should().Be(1);
        rows.Single(s => s.EndDate == null).Amount.Should().Be(1500m);
        rows.Single(s => s.Amount == 1000m).EndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangeSalary_WhenEmployeeFired_Returns422()
    {
        
    }

    [Fact]
    public async Task ChangeSalary_WhenEmployeeNotFound_Returns404()
    {
        
    }

    [Fact]
    public async Task ChangeSalary_WithNonPositiveAmount_Returns400()
    {
        
    }

    [Fact]
    public async Task GetSalaryHistory_ReturnsAllSalariesOrderedByEffectiveDate()
    {
        
    }
}