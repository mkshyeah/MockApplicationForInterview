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

public class EmployeeCreateTests : IntegrationTestBase
{
    public EmployeeCreateTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateEmployee_WithValidData_Returns201AndPersists()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        //ACT
        var resp = await Client.PostAsJsonAsync("v1/employees", new
        {
            firstName = "Emp", lastName = "Test", email = "emp@test.com",
            departmentId = seedDepartmentId, positionId = seedPositionId,
            salary = 1000m, salaryType = "Monthly", hireDate = "2026-01-01T00:00:00Z"
        });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<EmployeeResponse>(Json);
        body!.FullName.Should().Be("Emp Test");
        body.Email.Should().Be("emp@test.com");
        body.CurrentSalary.Should().Be(1000m);
        body.Status.Should().Be("Active");

        var persisted = await WithDbContextAsync(db =>
            db.Set<EmployeeEntity>().SingleOrDefaultAsync(e => e.Id == body.Id));

        persisted.Should().NotBeNull();
        persisted!.Email.Should().Be("emp@test.com");
    }

    [Fact]
    public async Task CreateEmployee_CreatesInitialActiveSalary()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        //ACT
        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        //ASSERT
        var salaries = await WithDbContextAsync(db =>
            db.Set<SalaryEntity>()
                .Where(s => s.EmployeeId == body.Id)
                .ToListAsync());

        salaries.Should().HaveCount(1);
        salaries.Single().Amount.Should().Be(1000m);
        salaries.Single().EndDate.Should().BeNull();
    }

    [Fact]
    public async Task CreateEmployee_ThenGetById_ReturnsSameEmployee()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        //ACT
        var body = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var resp2 = await Client.GetAsync($"v1/employees/{body.Id}");
        var body2 = await resp2.Content.ReadFromJsonAsync<EmployeeResponse>(Json);

        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        body2.Should().BeEquivalentTo(body);
    }

    [Fact]
    public async Task CreateEmployee_WithDuplicateEmail_Returns409()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        //ACT
        // baseline-сотрудник — сам POST здесь не предмет теста, поэтому через хелпер
        await CreateEmployeeAsync(seedDepartmentId, seedPositionId, email: "emp@test.com");

        // конфликтующий POST — именно он проверяется тестом, оставлен вручную
        var resp2 = await Client.PostAsJsonAsync("v1/employees", new
        {
            firstName = "Emp2", lastName = "Test", email = "emp@test.com",
            departmentId = seedDepartmentId, positionId = seedPositionId,
            salary = 1200m, salaryType = "Monthly", hireDate = "2026-01-01T00:00:00Z"
        });

        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var count = await WithDbContextAsync(db =>
            db.Set<EmployeeEntity>().CountAsync(e => e.Email == "emp@test.com"));
        count.Should().Be(1);
    }

    [Fact]
    public async Task CreateEmployee_WithNonexistentDepartment_Returns404()
    {
        //ARRANGE
        var (_, seedPositionId) = await SeedReferenceDataAsync();
        var missingDepartmentId = Guid.NewGuid();

        //ACT
        var resp = await Client.PostAsJsonAsync("v1/employees", new
        {
            firstName = "Emp", lastName = "Test", email = "emp@test.com",
            departmentId = missingDepartmentId, positionId = seedPositionId,
            salary = 1000m, salaryType = "Monthly", hireDate = "2026-01-01T00:00:00Z"
        });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        problem!.Detail.Should().ContainEquivalentOf("department");
    }
}