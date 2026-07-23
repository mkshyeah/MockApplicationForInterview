using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;


public class GetEmployeesTests : IntegrationTestBase
{
    public GetEmployeesTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetEmployees_WithOffsetAndLimit_ReturnsPagedSubset()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        for (int i = 0; i < 5; i++)
        {
            await CreateEmployeeAsync(seedDepartmentId, seedPositionId, firstName: $"Emp{i}");
        }
        
        //ACT
        var resp2 = await Client.GetAsync("v1/employees?offset=2&limit=2");
        
        //ASSERT
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await resp2.Content.ReadFromJsonAsync<PagedResponse<EmployeeResponse>>(Json);
        
        page!.Total.Should().Be(5);
        page.Offset.Should().Be(2);
        page.Limit.Should().Be(2);
        page.Returned.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.Select(e => e.FullName).Should().Equal("Emp2 Test", "Emp3 Test");
        page.Items.Should().OnlyContain(e => e.CurrentSalary != null && e.Department != null);
    }

    [Fact]
    public async Task GetEmployee_WhenNotFound_Returns404()
    {
        //ARRANGE
        var missingId = Guid.NewGuid();
        
        //ACT
        var resp = await Client.GetAsync($"v1/employees/{missingId}");
        
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

    }

    [Fact]
    public async Task GetEmployees_WithDuplicateSortKey_PagesCoverAllRowsWithoutDuplicatesOrGaps()
    {
        // ARRANGE
        const int total = 25;
        const int limit = 5;
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();

        var seededIds = new List<Guid>();
        for (int i = 0; i < total; i++)
        {
            var employeeResponse = await CreateEmployeeAsync(seedDepartmentId, seedPositionId, firstName: "Emp");
            seededIds.Add(employeeResponse.Id);
        }

        // ACT
        var collectedIds = new List<Guid>();
        for (var offset = 0; offset < total; offset += limit)
        {
            var resp = await Client.GetAsync(
                $"v1/employees?offset={offset}&limit={limit}&orderby={EmployeeOrderBy.Name}&direction={SortDirection.Ascending}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var page = await resp.Content.ReadFromJsonAsync<PagedResponse<EmployeeResponse>>(Json);
            collectedIds.AddRange(page!.Items.Select(e => e.Id));
        }

        // Assert
        collectedIds.Should().HaveCount(total);
        collectedIds.Should().OnlyHaveUniqueItems();
        collectedIds.Should().BeEquivalentTo(seededIds);
    }
}