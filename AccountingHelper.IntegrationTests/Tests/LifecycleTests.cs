using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class LifecycleTests : IntegrationTestBase
{
    public LifecycleTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task FireEmployee_WhenActive_SetsFiredStatusAndClosesSalary()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp2 = await Client.PatchAsync($"v1/employees/{body.Id}/fire", content: null);

        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var employee = await WithDbContextAsync(db =>
            db.Set<EmployeeEntity>().SingleOrDefaultAsync(e => e.Id == body.Id));

        employee!.Status.Should().Be(EmployeeStatus.Fired);
        employee.TerminationDate.Should().NotBeNull();

        var salaries = await WithDbContextAsync(db =>
            db.Set<SalaryEntity>()
                .Where(s => s.EmployeeId == body.Id)
                .ToListAsync());
        
        salaries.Should().NotBeNull();
        salaries.Should().OnlyContain(s => s.EndDate != null);
    }

    [Fact]
    public async Task FireEmployee_WhenAlreadyFired_Returns422()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp2 = await Client.PatchAsync($"v1/employees/{body.Id}/fire", content: null);
        var resp3 = await Client.PatchAsync($"v1/employees/{body.Id}/fire", content: null);

        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        resp3.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SendOnVacation_WhenActive_SetsOnVacationStatus()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp2 = await Client.PatchAsync($"v1/employees/{body.Id}/on-vacation", content: null);

        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var employee = await WithDbContextAsync(db =>
            db.Set<EmployeeEntity>().SingleOrDefaultAsync(e => e.Id == body.Id));

        employee!.Status.Should().Be(EmployeeStatus.OnVacation);
        
        var salaries = await WithDbContextAsync(db =>
            db.Set<SalaryEntity>()
                .Where(s => s.EmployeeId == body.Id)
                .ToListAsync());

        salaries.Should().ContainSingle(s => s.EndDate == null);
    }

    [Fact]
    public async Task SendOnVacation_WhenFired_Returns422()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp2 = await Client.PatchAsync($"v1/employees/{body.Id}/fire", content: null);
        var resp3 = await Client.PatchAsync($"v1/employees/{body.Id}/on-vacation", content: null);
        
        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        resp3.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        
        var problem = await resp3.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("Cannot send a fired employee on vacation.");
        
        var employee = await WithDbContextAsync(db =>
            db.Set<EmployeeEntity>().SingleAsync(e => e.Id == body.Id));

        employee.Status.Should().Be(EmployeeStatus.Fired);
    }

    [Fact]
    public async Task SendOffVacation_WhenNotOnVacation_Returns422()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ACT
        var resp2 = await Client.PatchAsync($"v1/employees/{body.Id}/off-vacation", content: null);
        
        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        
        var problem = await resp2.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("is not currently on vacation.");
        
        var employee = await WithDbContextAsync(db =>
            db.Set<EmployeeEntity>().SingleAsync(e => e.Id == body.Id));

        employee.Status.Should().Be(EmployeeStatus.Active);
    }
}