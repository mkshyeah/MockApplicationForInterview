using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Data.Entities;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class RejectLeaveRequestTests : IntegrationTestBase
{
    public RejectLeaveRequestTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task RejectLeaveRequest_WithPendingRequest_Returns200AndLeavesBalanceUntouched()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var leaveRequest = await SubmitLeaveRequestAsync(employee.Id);

        //ACT
        var resp = await Client.PatchAsync($"v1/leave-requests/{leaveRequest.Id}/reject", content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        body!.Id.Should().Be(leaveRequest.Id);
        body.Status.Should().Be(LeaveStatus.Rejected);
        body.DecidedAt.Should().NotBeNull();

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Rejected);
        row.DecidedAt.Should().NotBeNull();

        await AssertBalancesAreUntouched(employee.Id);
    }

    [Fact]
    public async Task RejectLeaveRequest_WhenAlreadyRejected_Returns422()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var leaveRequest = await SubmitLeaveRequestAsync(employee.Id);

        var firstRejection = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest.Id}/reject", content: null);
        firstRejection.StatusCode.Should().Be(HttpStatusCode.OK);

        // снимок из БД: сравнение БД с БД не зависит от точности round-trip'а через JSON
        var decidedAt = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.Id == leaveRequest.Id)
            .Select(l => l.DecidedAt)
            .SingleAsync());

        //ACT
        var resp = await Client.PatchAsync($"v1/leave-requests/{leaveRequest.Id}/reject", content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("rejected");

        // отклонённый вызов не должен был переписать отметку о решении
        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Rejected);
        row.DecidedAt.Should().Be(decidedAt);

        await AssertBalancesAreUntouched(employee.Id);
    }

    [Fact]
    public async Task RejectLeaveRequest_WhenAlreadyApproved_Returns422AndDoesNotReturnDebitedDays()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var leaveRequest = await SubmitLeaveRequestAsync(employee.Id);

        var approval = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest.Id}/approve", content: null);
        approval.StatusCode.Should().Be(HttpStatusCode.OK);

        //ACT
        var resp = await Client.PatchAsync($"v1/leave-requests/{leaveRequest.Id}/reject", content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("approved");

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Approved);

        // отмены одобрения не существует: списанные дни обратно не начисляются
        var balances = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .Where(b => b.EmployeeId == employee.Id)
            .ToListAsync());

        balances.Single(b => b.LeaveType == LeaveType.Annual).RemainingDays.Should().Be(23); // 28 - 5
        balances.Single(b => b.LeaveType == LeaveType.Sick).RemainingDays.Should().Be(14);
        balances.Single(b => b.LeaveType == LeaveType.Unpaid).RemainingDays.Should().Be(30);
    }

    [Fact]
    public async Task RejectLeaveRequest_WhenRequestNotFound_Returns404()
    {
        //ARRANGE
        var missingId = Guid.NewGuid();

        //ACT
        var resp = await Client.PatchAsync($"v1/leave-requests/{missingId}/reject", content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().Contain(missingId.ToString());
    }

    private async Task<LeaveRequestResponse> SubmitLeaveRequestAsync(Guid employeeId)
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employeeId}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = start.AddDays(4) }); // 5 дней

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        return body!;
    }

    private async Task AssertBalancesAreUntouched(Guid employeeId)
    {
        var balances = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .Where(b => b.EmployeeId == employeeId)
            .ToListAsync());

        balances.Single(b => b.LeaveType == LeaveType.Annual).RemainingDays.Should().Be(28);
        balances.Single(b => b.LeaveType == LeaveType.Sick).RemainingDays.Should().Be(14);
        balances.Single(b => b.LeaveType == LeaveType.Unpaid).RemainingDays.Should().Be(30);
    }
}
