using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ReferenceDataTests : IntegrationTestBase
{
    public ReferenceDataTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetDepartments_ReturnsAllDepartmentsOrderedByName()
    {
        // ARRANGE — сидируем не по алфавиту, чтобы порядок вставки не совпал с ожидаемым
        var salesId = await SeedDepartmentAsync("Sales");
        var financeId = await SeedDepartmentAsync("Finance");
        var itId = await SeedDepartmentAsync("IT");

        // ACT
        var resp = await Client.GetAsync("v1/departments");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var departments = await resp.Content.ReadFromJsonAsync<List<DepartmentResponse>>(Json);

        departments!.Select(d => (d.Id, d.Name))
            .Should().Equal(
                (financeId, "Finance"),
                (itId, "IT"),
                (salesId, "Sales"));
    }

    [Fact]
    public async Task GetDepartments_WhenNoneExist_ReturnsEmptyList()
    {
        // ACT
        var resp = await Client.GetAsync("v1/departments");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var departments = await resp.Content.ReadFromJsonAsync<List<DepartmentResponse>>(Json);

        // пустой справочник — это пустой список, а не 404
        departments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPositions_ReturnsAllPositionsOrderedByTitle_WithGradeAsString()
    {
        // ARRANGE
        var seniorId = await SeedPositionAsync("Senior Developer", EmployeeGrade.Senior);
        var juniorId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        var leadId = await SeedPositionAsync("Lead Developer", EmployeeGrade.Lead);

        // ACT
        var resp = await Client.GetAsync("v1/positions");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var raw = await resp.Content.ReadAsStringAsync();
        var positions = JsonSerializer.Deserialize<List<PositionResponse>>(raw, Json);

        positions!.Select(p => (p.Id, p.Title, p.Grade))
            .Should().Equal(
                (juniorId, "Junior Developer", EmployeeGrade.Junior),
                (leadId, "Lead Developer", EmployeeGrade.Lead),
                (seniorId, "Senior Developer", EmployeeGrade.Senior));

        // грейд уезжает строкой, а не числом: в DTO это enum, и без StrictEnumConverter
        // в JSON ушла бы цифра
        raw.Should().Contain("\"grade\":\"Junior\"");
    }

    [Fact]
    public async Task GetPositions_WhenNoneExist_ReturnsEmptyList()
    {
        // ACT
        var resp = await Client.GetAsync("v1/positions");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var positions = await resp.Content.ReadFromJsonAsync<List<PositionResponse>>(Json);

        positions.Should().BeEmpty();
    }
}
