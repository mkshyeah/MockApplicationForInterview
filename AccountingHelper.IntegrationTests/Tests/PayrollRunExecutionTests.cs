using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Payroll.Commands.RunPayroll;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

// The consumer is a three-line adapter over ISender, and that the broker delivers at all is already
// covered by MessagingRoundTripTests. So these tests dispatch the command directly and spend their
// time on what only a real database can answer: the population filter and the unique index.
public class PayrollRunExecutionTests : IntegrationTestBase
{
    public PayrollRunExecutionTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private static readonly DateTime Period = DateTime.UtcNow.AddMonths(-1);

    private async Task<Guid> CreateRunAsync()
    {
        var resp = await Client.PostAsJsonAsync(
            "v1/payroll-runs",
            new { year = Period.Year, month = Period.Month });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        return (await resp.Content.ReadFromJsonAsync<PayrollRunResponse>(Json))!.Id;
    }

    private async Task RunPayrollAsync(Guid runId)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new RunPayrollCommand(runId));
    }

    [Fact]
    public async Task RunPayroll_PaysTheEmployed_SkipsSalaryless_AndIgnoresFired()
    {
        // ARRANGE
        var departmentId = await SeedDepartmentAsync("IT");
        var positionId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);

        var paid = await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);
        var salaryless = await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);
        var fired = await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);

        // Closing the salary without touching the status is the hole phase 3 wrote down: firing is
        // a handler convention, not a schema constraint, so this state is reachable from an import.
        await WithDbContextAsync(async db =>
        {
            var salary = await db.Salaries.SingleAsync(s => s.EmployeeId == salaryless.Id && s.EndDate == null);
            salary.EndDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
        });

        (await Client.PatchAsync($"v1/employees/{fired.Id}/fire", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var runId = await CreateRunAsync();

        // ACT
        await RunPayrollAsync(runId);

        // ASSERT
        await WithDbContextAsync(async db =>
        {
            var run = await db.PayrollRuns.SingleAsync(r => r.Id == runId);

            run.Status.Should().Be(PayrollRunStatus.Completed);
            run.StartedAt.Should().NotBeNull();
            run.CompletedAt.Should().NotBeNull();

            // One paid, one walked past. The fired employee is in neither count: he left the
            // population entirely, which is the same set the dashboard counts.
            run.ProcessedCount.Should().Be(1);
            run.SkippedCount.Should().Be(1);

            var payslip = await db.Payslips.SingleAsync();

            payslip.EmployeeId.Should().Be(paid.Id);
            payslip.Gross.Should().Be(1_000.00m);
            payslip.Tax.Should().Be(100.00m);
            payslip.Net.Should().Be(900.00m);
        });
    }

    [Fact]
    public async Task GetPayslips_AfterRun_ReturnsOneSlipPerPaidEmployee()
    {
        // ARRANGE
        var departmentId = await SeedDepartmentAsync("IT");
        var positionId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        var employee = await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);

        var runId = await CreateRunAsync();
        await RunPayrollAsync(runId);

        // ACT
        var resp = await Client.GetAsync($"v1/payroll-runs/{runId}/payslips");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payslips = await resp.Content.ReadFromJsonAsync<List<PayslipResponse>>(Json);
        var payslip = payslips.Should().ContainSingle().Subject;

        payslip.EmployeeId.Should().Be(employee.Id);
        payslip.Gross.Should().Be(1_000.00m);
        payslip.Tax.Should().Be(100.00m);
        payslip.Net.Should().Be(900.00m);
    }

    [Fact]
    public async Task GetPayslips_WhenRunDoesNotExist_Returns404()
    {
        // ACT
        var resp = await Client.GetAsync($"v1/payroll-runs/{Guid.NewGuid()}/payslips");

        // ASSERT
        // 404 rather than an empty list: "this run has no payslips" and "there is no such run" are
        // answers to different questions, and a caller polling a typo deserves to be told.
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RunPayroll_WhenTheSameRunIsProcessedTwice_DoesNotDoublePayslips()
    {
        // ARRANGE
        var departmentId = await SeedDepartmentAsync("IT");
        var positionId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);

        var runId = await CreateRunAsync();
        await RunPayrollAsync(runId);

        // Rewinding the status defeats the handler's fast path on purpose. Without this the second
        // pass would return at the status check and the unique index would never be exercised —
        // the test would pass while proving nothing about at-least-once delivery.
        await WithDbContextAsync(async db =>
        {
            var run = await db.PayrollRuns.SingleAsync(r => r.Id == runId);
            run.Status = PayrollRunStatus.Processing;
            await db.SaveChangesAsync();
        });

        // ACT
        var act = () => RunPayrollAsync(runId);

        // ASSERT
        // A redelivery is not a failure: the money is already paid, so the handler swallows the
        // index violation and acknowledges instead of dead-lettering a run that succeeded.
        await act.Should().NotThrowAsync();

        await WithDbContextAsync(async db =>
            (await db.Payslips.CountAsync(p => p.PayrollRunId == runId)).Should().Be(1));
    }
}
