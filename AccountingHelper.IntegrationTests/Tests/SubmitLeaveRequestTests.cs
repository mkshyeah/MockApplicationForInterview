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

public class SubmitLeaveRequestTests : IntegrationTestBase
{
    public SubmitLeaveRequestTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SubmitLeaveRequest_WithValidData_Return201AndPersists()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);
        
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(4);
        
        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        body.Should().NotBeNull();
        body!.EmployeeId.Should().Be(employee.Id);
        body.LeaveType.Should().Be(LeaveType.Annual);
        body.Status.Should().Be(LeaveStatus.Pending);
        body.DurationInDays.Should().Be(5);

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.EmployeeId == employee.Id));
        
        row.Status.Should().Be(LeaveStatus.Pending);
        row.LeaveType.Should().Be(LeaveType.Annual);
        row.StartDate.Should().Be(start);
        row.EndDate.Should().Be(end);
        
        var followUp = await Client.GetAsync(resp.Headers.Location);
        followUp.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var balance = await WithDbContextAsync(db => db.Set<LeaveBalanceEntity>()
            .SingleAsync(b => b.EmployeeId == employee.Id && b.LeaveType == LeaveType.Annual));
        balance.RemainingDays.Should().Be(LeaveEntitlement.DaysFor(LeaveType.Annual));
    }
    
    [Theory]
    [InlineData("Nonsense")]
    [InlineData(99)]
    // "3" and "Annual, Sick" both resolve to Unpaid via Enum.Parse — names only, no numbers,
    // no bitwise combinations
    [InlineData("3")]
    [InlineData("Annual, Sick")]
    public async Task SubmitLeaveRequest_WhenLeaveTypeIsUnknownValue_Returns400WithAllowedValues(object badLeaveType)
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(4);

        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new Dictionary<string, object?>
            {
                ["leaveType"] = badLeaveType,
                ["startDate"] = start,
                ["endDate"] = end
            });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem.Should().NotBeNull();

        // same envelope as a FluentValidation failure, no traceId and no noise about "request"
        problem!.Title.Should().Be("Validation Failed");
        problem.Extensions.Should().ContainKey("correlationId");
        problem.Errors.Should().ContainSingle();

        // same key the client sent, regardless of which layer rejected the value
        problem.Errors.Should().ContainKey("leaveType");

        // exact match on purpose: it also pins that no CLR type name and no
        // "Path: ... | LineNumber: ..." tail from the serializer leaks into the contract
        problem.Errors["leaveType"].Should().ContainSingle()
            .Which.Should().Be("must be one of: Annual, Sick, Unpaid");

        var rows = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.EmployeeId == employee.Id)
            .ToListAsync());
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitLeaveRequest_WhenEmployeeNotFound_Returns404()
    {
        //ARRANGE
        var missingId = Guid.NewGuid();
        
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(4);
        //ACT
         var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{missingId}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });
        
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        problem!.Detail.Should().ContainEquivalentOf("employee");
    }
    
    [Fact]
    public async Task SubmitLeaveRequest_WhenEndDateLessThanStart_Returns400()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(-1);
        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });
        
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Should().NotBeNull();
        problem.Errors.Should().ContainSingle();

        problem.Errors.Should().ContainKey("endDate");

        var message = problem.Errors["endDate"];
        message.Should().ContainSingle()
            .Which.Should().Be("End date must be on or after start date.");

        var rows = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.EmployeeId == employee.Id)
            .ToListAsync());
        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitLeaveRequest_WhenDurationIsExactlyMaxAllowed_Returns201()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(364); // 365 дней вместе с границами — верхняя допустимая длина

        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        //ASSERT
        // предел проверяется при подаче; хватит ли дней — вопрос одобрения, не подачи
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await resp.Content.ReadFromJsonAsync<LeaveRequestResponse>(Json);
        body!.DurationInDays.Should().Be(365);
    }

    [Fact]
    public async Task SubmitLeaveRequest_WhenDurationExceedsMaxAllowed_Returns400()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);
        var end = start.AddDays(365); // 366 дней — на один больше предела

        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainSingle();

        problem.Errors.Should().ContainKey("endDate");
        problem.Errors["endDate"].Should().ContainSingle()
            .Which.Should().Be("Leave cannot exceed 365 days.");

        var rows = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.EmployeeId == employee.Id)
            .ToListAsync());
        rows.Should().BeEmpty();
    }

    /// <summary>
    /// The earliest start date the validator accepts. Recomputed per test rather than
    /// cached in a static field so that it matches what the API sees at request time.
    /// </summary>
    private static DateOnly EarliestAllowedStart => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));

    [Fact]
    public async Task SubmitLeaveRequest_WhenStartDateIsExactlyOneMonthAgo_Returns201()
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        // the boundary is inclusive: this is the oldest start date still allowed
        var start = EarliestAllowedStart;
        var end = start.AddDays(10);

        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var row = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .SingleAsync(l => l.EmployeeId == employee.Id));
        row.StartDate.Should().Be(start);
        row.Status.Should().Be(LeaveStatus.Pending);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-30)]
    public async Task SubmitLeaveRequest_WhenStartDateIsEarlierThanOneMonthAgo_Returns400(int daysBeforeEarliest)
    {
        //ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        var employee = await CreateEmployeeAsync(departmentId, positionId);

        var start = EarliestAllowedStart.AddDays(daysBeforeEarliest);
        var end = start.AddDays(10);

        //ACT
        var resp = await Client.PostAsJsonAsync(
            $"v1/employees/{employee.Id}/leave-requests",
            new { leaveType = "Annual", startDate = start, endDate = end });

        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainSingle();

        problem.Errors.Should().ContainKey("startDate");
        problem.Errors["startDate"].Should().ContainSingle()
            .Which.Should().Be("Start date must not be earlier than one month ago.");

        var rows = await WithDbContextAsync(db => db.Set<LeaveRequestEntity>()
            .Where(l => l.EmployeeId == employee.Id)
            .ToListAsync());
        rows.Should().BeEmpty();
    }
}