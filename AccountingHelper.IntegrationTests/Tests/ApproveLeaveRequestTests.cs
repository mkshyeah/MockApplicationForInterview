using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Models;
using AccountingHelper.Infrastructure.Data.Entities;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ApproveLeaveRequestTests : IntegrationTestBase
{
    public ApproveLeaveRequestTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ApproveLeaveRequest_WithPendingRequest_Returns200AndDebitsBalance()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(4); // 5 календарных дней вместе с границами

        var subResp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        subResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var leaveRequest = await subResp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);

        //ACT
        var resp = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest!.Id}/approve",
            content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var respContent = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        respContent!.Id.Should().Be(leaveRequest.Id);
        respContent.Status.Should().Be(LeaveStatus.Approved);
        respContent.DecidedAt.Should().NotBeNull();

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Approved);
        row.DecidedAt.Should().NotBeNull();

        var balances = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .Where(b => b.EmployeeId == employee.Id)
            .ToListAsync());

        // 28 - 5
        balances.Single(b => b.LeaveType == LeaveType.Annual).RemainingDays.Should().Be(23);
        
        balances.Single(b => b.LeaveType == LeaveType.Sick).RemainingDays.Should().Be(14);
        balances.Single(b => b.LeaveType == LeaveType.Unpaid).RemainingDays.Should().Be(30);
    }
    
    [Fact]
    public async Task ApproveLeaveRequest_WhenAlreadyApproved_DoesNotDebitBalanceTwice()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(4); // 5 календарных дней вместе с границами

        var subResp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        subResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var leaveRequest = await subResp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);

        var firstApproval = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest!.Id}/approve",
            content: null);
        firstApproval.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var decidedAt = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.Id == leaveRequest.Id)
            .Select(l => l.DecidedAt)
            .SingleAsync());
        decidedAt.Should().NotBeNull();

        //ACT
        var duplicateResp = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest.Id}/approve",
            content: null);

        //ASSERT
        duplicateResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await duplicateResp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("already approved");

        // отклонённый вызов не должен был тронуть заявку — включая отметку о решении
        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Approved);
        row.DecidedAt.Should().Be(decidedAt);

        var balances = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .Where(b => b.EmployeeId == employee.Id)
            .ToListAsync());

        // 28 - 5, списание ровно одно
        balances.Single(b => b.LeaveType == LeaveType.Annual).RemainingDays.Should().Be(23);
        balances.Single(b => b.LeaveType == LeaveType.Sick).RemainingDays.Should().Be(14);
        balances.Single(b => b.LeaveType == LeaveType.Unpaid).RemainingDays.Should().Be(30);
    }
    
    [Fact]
    public async Task ApproveLeaveRequest_WhenBalanceIsInsufficient_Returns422AndLeavesBalanceUntouched()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(30); // 31 день против 28 доступных

        var subResp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        subResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var leaveRequest = await subResp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);

        //ACT
        var resp = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest!.Id}/approve",
            content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("remaining");

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Pending);
        row.DecidedAt.Should().BeNull();

        var balances = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .Where(b => b.EmployeeId == employee.Id)
            .ToListAsync());

        // ни одного частичного списания
        balances.Single(b => b.LeaveType == LeaveType.Annual).RemainingDays.Should().Be(28);
        balances.Single(b => b.LeaveType == LeaveType.Sick).RemainingDays.Should().Be(14);
        balances.Single(b => b.LeaveType == LeaveType.Unpaid).RemainingDays.Should().Be(30);
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenRequestNotFound_Returns404()
    {
        //ARRANGE
        var missingId = Guid.NewGuid();

        //ACT
        var resp = await Client.PatchAsync($"v1/leave-requests/{missingId}/approve", content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task ApproveLeaveRequest_WhenEmployeeIsFired_Returns422AndLeavesBalanceUntouched()
    {
        //ARRANGE
        var (seedDepartmentId, seedPositionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(seedDepartmentId, seedPositionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(4);

        var subResp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        subResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var leaveRequest = await subResp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);

        // заявка подана, пока сотрудник работал, — увольнение происходит между подачей и решением
        var fire = await Client.PatchAsync($"v1/employees/{employee.Id}/fire", content: null);
        fire.StatusCode.Should().Be(HttpStatusCode.OK);

        //ACT
        var resp = await Client.PatchAsync(
            $"v1/leave-requests/{leaveRequest!.Id}/approve",
            content: null);

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Detail.Should().ContainEquivalentOf("fired");

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.Id == leaveRequest.Id));
        row.Status.Should().Be(LeaveStatus.Pending);
        row.DecidedAt.Should().BeNull();

        var balance = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .SingleAsync(b => b.EmployeeId == employee.Id && b.LeaveType == LeaveType.Annual));
        balance.RemainingDays.Should().Be(28);
    }
    
    [Fact]
    public async Task LeaveBalance_WhenTwoContextsUpdateTheSameRow_SecondSaveThrowsConcurrencyException()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        //ACT + ASSERT
        // два независимых DbContext, каждый со своим ChangeTracker и своим снимком xmin
        await WithDbContextAsync(async firstContext =>
        {
            await WithDbContextAsync(async secondContext =>
            {
                // отслеживаемое чтение: без него EF не запомнит версию строки
                var firstBalance = await firstContext.Set<LeaveBalanceEntity>()
                    .SingleAsync(b => b.EmployeeId == employee.Id && b.LeaveType == LeaveType.Annual);

                var secondBalance = await secondContext.Set<LeaveBalanceEntity>()
                    .SingleAsync(b => b.EmployeeId == employee.Id && b.LeaveType == LeaveType.Annual);

                firstBalance.RemainingDays -= 5;  // 28 -> 23
                secondBalance.RemainingDays -= 3; // 28 -> 25, на устаревшей версии

                await firstContext.SaveChangesAsync();

                // xmin строки изменился, WHERE второго UPDATE больше ни на что не попадёт
                var act = async () => await secondContext.SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
            });
        });
        
        var remainingDays = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .Where(b => b.EmployeeId == employee.Id && b.LeaveType == LeaveType.Annual)
            .Select(b => b.RemainingDays)
            .SingleAsync());

        remainingDays.Should().Be(23);
    }
    
    [Fact]
    public async Task ApproveLeaveRequest_WhenTwoRequestsRaceForOneBalance_DebitsOnlyOne()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // по 20 дней каждая при квоте 28: по отдельности проходит любая, вместе — нет
        var first = await SubmitLeaveRequestAsync(employee.Id, today.AddDays(7), days: 20);
        var second = await SubmitLeaveRequestAsync(employee.Id, today.AddDays(60), days: 20);

        //ACT
        var responses = await Task.WhenAll(
            Client.PatchAsync($"v1/leave-requests/{first.Id}/approve", content: null),
            Client.PatchAsync($"v1/leave-requests/{second.Id}/approve", content: null));

        //ASSERT
        // ровно один победитель — детерминировано при любом расписании
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);

        // проигравший мог не успеть прочитать баланс (409) или прочитать уже списанный (422)
        responses.Single(r => r.StatusCode != HttpStatusCode.OK)
            .StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity);

        var rows = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.EmployeeId == employee.Id)
            .ToListAsync());

        rows.Should().HaveCount(2);
        rows.Count(l => l.Status == LeaveStatus.Approved).Should().Be(1);
        rows.Count(l => l.Status == LeaveStatus.Pending).Should().Be(1);

        // главный инвариант: списание ровно одно, 28 - 20
        var balance = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .SingleAsync(b => b.EmployeeId == employee.Id && b.LeaveType == LeaveType.Annual));

        balance.RemainingDays.Should().Be(8);
    }

    private async Task<LeaveRequestResponse> SubmitLeaveRequestAsync(Guid employeeId, DateOnly start, int days)
    {
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employeeId}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = start.AddDays(days - 1) });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        return body!;
    }
}
    
    
