using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class GetLeaveRequestsTests : IntegrationTestBase
{
    public GetLeaveRequestsTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetLeaveRequests_ReturnsOnlyRequestsOfRequestedEmployee()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);
        var otherEmployee = await CreateEmployeeAsync(departmentId, positionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var own = await SubmitLeaveRequestAsync(employee.Id, start);
        var alsoOwn = await SubmitLeaveRequestAsync(employee.Id, start.AddDays(20), LeaveType.Sick);
        var foreign = await SubmitLeaveRequestAsync(otherEmployee.Id, start);

        // ACT
        var body = await Client.GetFromJsonAsync<List<LeaveRequestResponse>>(
            $"v1/employees/{employee.Id}/leave-requests", Json);

        // ASSERT
        body.Should().NotBeNull();
        body!.Should().OnlyContain(r => r.EmployeeId == employee.Id);
        body.Select(r => r.Id).Should().BeEquivalentTo(new[] { own.Id, alsoOwn.Id });
        body.Select(r => r.Id).Should().NotContain(foreign.Id);
    }

    [Fact]
    public async Task GetLeaveRequests_ReturnsNewestFirst()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        // submitted in this order, so RequestedAt grows with each call
        var first = await SubmitLeaveRequestAsync(employee.Id, start);
        var second = await SubmitLeaveRequestAsync(employee.Id, start.AddDays(20));
        var third = await SubmitLeaveRequestAsync(employee.Id, start.AddDays(40));

        // ACT
        var body = await Client.GetFromJsonAsync<List<LeaveRequestResponse>>(
            $"v1/employees/{employee.Id}/leave-requests", Json);

        // ASSERT
        // OrderByDescending(RequestedAt).ThenBy(Id) — the ids must come back reversed
        body!.Select(r => r.Id).Should().ContainInOrder(third.Id, second.Id, first.Id);
        body.Should().BeInDescendingOrder(r => r.RequestedAt);
    }

    [Fact]
    public async Task GetLeaveRequests_WhenEmployeeHasNoRequests_ReturnsEmptyList()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        // ACT
        var resp = await Client.GetAsync($"v1/employees/{employee.Id}/leave-requests");

        // ASSERT
        // an employee without requests is not a missing employee
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<List<LeaveRequestResponse>>(Json);
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLeaveRequests_WhenEmployeeNotFound_Returns404()
    {
        // ARRANGE
        var missingId = Guid.NewGuid();

        // ACT
        var resp = await Client.GetAsync($"v1/employees/{missingId}/leave-requests");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("employee");
    }

    private async Task<LeaveRequestResponse> SubmitLeaveRequestAsync(
        Guid employeeId,
        DateOnly start,
        LeaveType leaveType = LeaveType.Annual)
    {
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employeeId}/leave-requests",
            new { leaveType = leaveType.ToString(), startDate = start, endDate = start.AddDays(2) });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        return body!;
    }
}
