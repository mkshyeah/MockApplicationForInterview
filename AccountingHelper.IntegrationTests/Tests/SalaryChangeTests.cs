using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Infrastructure.Data.Entities;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp = await Client.PutAsJsonAsync(
            $"v1/employees/{employee.Id}/salaries",
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
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var fire = await Client.PatchAsync(
            $"v1/employees/{employee.Id}/fire", content: null);
        fire.StatusCode.Should().Be(HttpStatusCode.OK);
 
        //ACT
        var resp = await Client.PutAsJsonAsync(
            $"v1/employees/{employee.Id}/salaries",
            new {amount = 1500m, salaryType = "Monthly"});
 
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
 
        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("fired"); // TODO: сверь с реальным текстом ошибки
 
        var salaries = await WithDbContextAsync(db =>
            db.Set<SalaryEntity>()
                .Where(s => s.EmployeeId == employee.Id)
                .ToListAsync());
 
        salaries.Should().ContainSingle();
        salaries.Single().Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task ChangeSalary_WhenEmployeeNotFound_Returns404()
    {
        //ARRANGE
        var missingEmployeeId = Guid.NewGuid();
 
        //ACT
        var resp = await Client.PutAsJsonAsync(
            $"v1/employees/{missingEmployeeId}/salaries",
            new { amount = 1500m, salaryType = "Monthly" });
 
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task ChangeSalary_WithNonPositiveAmount_Returns400(int amount)
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp = await Client.PutAsJsonAsync(
            $"v1/employees/{employee.Id}/salaries",
            new { amount, salaryType = "Monthly" });
 
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
 
        var salaries = await WithDbContextAsync(db =>
            db.Set<SalaryEntity>()
                .Where(s => s.EmployeeId == employee.Id)
                .ToListAsync());
 
        salaries.Should().ContainSingle(); 
        salaries.Single().Amount.Should().Be(1000m); 
    }

    [Fact]
    public async Task GetSalaryHistory_ReturnsAllSalariesOrderedByEffectiveDate()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var raise1 = await Client.PutAsJsonAsync(
            $"v1/employees/{employee.Id}/salaries",
            new { amount = 1500m, salaryType = "Monthly" });
        raise1.StatusCode.Should().Be(HttpStatusCode.OK);
 
        var raise2 = await Client.PutAsJsonAsync(
            $"v1/employees/{employee.Id}/salaries",
            new { amount = 2000m, salaryType = "Monthly" });
        raise2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        //ACT
        
        var resp = await Client.GetAsync($"v1/employees/{employee.Id}/salaries");
        
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await resp.Content.ReadFromJsonAsync<IReadOnlyList<SalaryResponse>>(Json);
        
        history!.Should().HaveCount(3);
        history!.Select(s => s.Amount).Should().Equal(2000m, 1500m, 1000m);
        history!.Count(s => s.EndDate == null).Should().Be(1);
        history!.First().EndDate.Should().BeNull();
    }
}