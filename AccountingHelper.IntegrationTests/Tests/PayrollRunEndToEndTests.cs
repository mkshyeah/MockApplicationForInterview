using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace AccountingHelper.IntegrationTests.Tests;

// The only test that walks the production path whole: POST writes the run and an outbox row, the
// delivery service hands it to RabbitMQ, the consumer picks it up and dispatches the command.
// Every other payroll test skips straight to ISender, which is why a consumer that was never
// registered went unnoticed through an entire block.
public class PayrollRunEndToEndTests : IntegrationTestBase
{
    public PayrollRunEndToEndTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private static readonly DateTime Period = DateTime.UtcNow.AddMonths(-1);

    private async Task<PayrollRunResponse> WaitForRunAsync(Guid runId, PayrollRunStatus expected)
    {
        var stopwatch = Stopwatch.StartNew();
        PayrollRunResponse? last = null;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            var resp = await Client.GetAsync($"v1/payroll-runs/{runId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            last = await resp.Content.ReadFromJsonAsync<PayrollRunResponse>(Json);
            if (last!.Status == expected) return last;

            await Task.Delay(100);
        }

        throw new XunitException(
            $"Run {runId} did not reach {expected} within 30s. Last status: {last?.Status}.");
    }

    [Fact]
    public async Task PostPayrollRun_IsPickedUpByTheWorker_AndCompletes()
    {
        // ARRANGE
        // Started before the run is requested so the queue and its binding exist. Declare them
        // afterwards and the broker has already dropped the message at the exchange.
        await using var worker = await WorkerTestHost.StartAsync(
            Services.GetRequiredService<IConfiguration>());

        var departmentId = await SeedDepartmentAsync("IT");
        var positionId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        var employee = await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);

        // ACT
        var created = await Client.PostAsJsonAsync(
            "v1/payroll-runs",
            new { year = Period.Year, month = Period.Month });

        created.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var runId = (await created.Content.ReadFromJsonAsync<PayrollRunResponse>(Json))!.Id;

        // ASSERT
        var completed = await WaitForRunAsync(runId, PayrollRunStatus.Completed);

        completed.ProcessedCount.Should().Be(1);
        completed.SkippedCount.Should().Be(0);

        await WithDbContextAsync(async db =>
        {
            var payslip = await db.Payslips.SingleAsync(p => p.PayrollRunId == runId);

            payslip.EmployeeId.Should().Be(employee.Id);
            payslip.Gross.Should().Be(1_000.00m);
            payslip.Net.Should().Be(900.00m);
        });
    }
}
