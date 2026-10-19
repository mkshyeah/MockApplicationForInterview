using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

// The import's counterpart of PayrollRunEndToEndTests: the only test that walks the production path whole.
// POST writes the job, the file and an outbox row; the delivery service hands ImportRequested to RabbitMQ;
// ImportRunConsumer dispatches the command; the handler publishes ImportCompleted; CacheInvalidationConsumer
// evicts the dashboard. ImportRunExecutionTests dispatch through ISender and touch none of that wiring.
public class ImportEndToEndTests : IntegrationTestBase
{
    public ImportEndToEndTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Header = "FirstName,LastName,Email,Department,Position,SalaryAmount,SalaryType,HireDate\n";

    private async Task<Guid> UploadAsync(string csv)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");

        var resp = await Client.PostAsync("v1/imports", new MultipartFormDataContent { { file, "file", "employees.csv" } });
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        return (await resp.Content.ReadFromJsonAsync<ImportJobResponse>(Json))!.Id;
    }

    // Polls the status resource the way a client does; no Sleep after publishing.
    private async Task<ImportJobResponse> WaitForJobAsync(Guid jobId, ImportJobStatus expected)
    {
        var stopwatch = Stopwatch.StartNew();
        ImportJobResponse? last = null;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            var resp = await Client.GetAsync($"v1/imports/{jobId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            last = await resp.Content.ReadFromJsonAsync<ImportJobResponse>(Json);
            if (last!.Status == expected) return last;

            await Task.Delay(100);
        }

        throw new XunitException(
            $"Import job {jobId} did not reach {expected} within 30s. Last status: {last?.Status}.");
    }

    private async Task<DashboardResponse> GetDashboardAsync()
    {
        var resp = await Client.GetAsync("v1/reporting/dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await resp.Content.ReadFromJsonAsync<DashboardResponse>(Json))!;
    }

    private async Task<DashboardResponse> WaitForDashboardAsync(Func<DashboardResponse, bool> predicate)
    {
        var stopwatch = Stopwatch.StartNew();
        DashboardResponse? last = null;

        while (stopwatch.Elapsed < TimeSpan.FromSeconds(10))
        {
            last = await GetDashboardAsync();
            if (predicate(last)) return last;

            await Task.Delay(50);
        }

        throw new XunitException(
            $"Dashboard did not refresh within 10s. Last seen payroll: {last?.TotalMonthlyPayroll}.");
    }

    [Fact]
    public async Task PostImport_IsPickedUpByTheWorker_AndCompletes()
    {
        // ARRANGE
        // Started before the upload so the queue and its binding exist. Declared afterwards, the broker
        // would already have dropped ImportRequested at the exchange.
        await using var worker = await WorkerTestHost.StartAsync(
            Services.GetRequiredService<IConfiguration>());

        await SeedDepartmentAsync("Engineering");
        await SeedPositionAsync("Developer", EmployeeGrade.Junior);

        // ACT
        var jobId = await UploadAsync(Header +
                                      "Aigerim,Nurlanova,aigerim@import.test,Engineering,Developer,1000,Monthly,2024-01-15\n" +
                                      "Bad,Row,bad@import.test,Marketing,Developer,1000,Monthly,2024-01-15\n");

        // ASSERT
        var completed = await WaitForJobAsync(jobId, ImportJobStatus.Completed);

        completed.TotalRows.Should().Be(2);
        completed.ImportedCount.Should().Be(1);
        completed.FailedCount.Should().Be(1);

        await WithDbContextAsync(async db =>
        {
            var employee = await db.Employees.SingleAsync(e => e.Email == "aigerim@import.test");
            (await db.LeaveBalances.CountAsync(b => b.EmployeeId == employee.Id))
                .Should().Be(Enum.GetValues<LeaveType>().Length);
        });
    }

    // The test from the phase plan: one event at the end of the import, not one per employee. The dashboard
    // TTL in the test configuration is an hour, so a refreshed value has exactly one explanation.
    [Fact]
    public async Task PostImport_RefreshesTheDashboard_WithoutWaitingForTtl()
    {
        // ARRANGE
        await using var worker = await WorkerTestHost.StartAsync(
            Services.GetRequiredService<IConfiguration>());

        var departmentId = await SeedDepartmentAsync("Engineering");
        var positionId = await SeedPositionAsync("Developer", EmployeeGrade.Junior);
        await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);

        // Warm the cache, and wait out the EmployeeHired eviction the hire above triggers, so the value the
        // import has to displace is a cached one.
        await WaitForDashboardAsync(d => d.TotalMonthlyPayroll == 1_000.00m);

        // ACT
        var jobId = await UploadAsync(Header +
                                      "A,One,one@import.test,Engineering,Developer,2000,Monthly,2024-01-15\n" +
                                      "B,Two,two@import.test,Engineering,Developer,3000,Monthly,2024-01-15\n");

        await WaitForJobAsync(jobId, ImportJobStatus.Completed);

        // ASSERT
        var fresh = await WaitForDashboardAsync(d => d.TotalMonthlyPayroll == 6_000.00m);
        fresh.HeadcountByDepartment.Should().ContainSingle().Which.Headcount.Should().Be(3);
    }
}
