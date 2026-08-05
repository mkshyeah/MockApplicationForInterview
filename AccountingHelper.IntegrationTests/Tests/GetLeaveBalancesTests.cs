using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class GetLeaveBalancesTests : IntegrationTestBase
{
    public GetLeaveBalancesTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetLeaveBalances_ReturnsEntitlementForEveryLeaveType()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        // ACT
        var body = await Client.GetFromJsonAsync<List<LeaveBalanceResponse>>(
            $"v1/employees/{employee.Id}/leave-balances", Json);

        // ASSERT
        body.Should().NotBeNull();

        // spelled out rather than read from LeaveEntitlement.DaysFor: the test must fail when the
        // numbers change or a new LeaveType is left unseeded
        body!.Select(b => new { b.LeaveType, b.RemainingDays })
            .Should().BeEquivalentTo(new[]
            {
                new { LeaveType = LeaveType.Annual, RemainingDays = 28 },
                new { LeaveType = LeaveType.Sick, RemainingDays = 14 },
                new { LeaveType = LeaveType.Unpaid, RemainingDays = 30 }
            });

        body.Should().OnlyContain(b => b.EmployeeId == employee.Id);
        body.Select(b => b.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
    }

    [Fact]
    public async Task GetLeaveBalances_ReturnsOnlyBalancesOfRequestedEmployee()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);
        var otherEmployee = await CreateEmployeeAsync(departmentId, positionId);

        // ACT
        var body = await Client.GetFromJsonAsync<List<LeaveBalanceResponse>>(
            $"v1/employees/{employee.Id}/leave-balances", Json);

        // ASSERT
        // six rows exist in the table at this point, three of them belong to someone else
        body.Should().HaveCount(3);
        body!.Should().OnlyContain(b => b.EmployeeId == employee.Id);
        body.Should().NotContain(b => b.EmployeeId == otherEmployee.Id);
    }

    [Fact]
    public async Task GetLeaveBalances_WhenEmployeeNotFound_Returns404()
    {
        // ARRANGE
        var missingId = Guid.NewGuid();

        // ACT
        var resp = await Client.GetAsync($"v1/employees/{missingId}/leave-balances");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("employee");
    }
}
