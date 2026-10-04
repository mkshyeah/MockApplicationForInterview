using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Features.Imports;
using AccountingHelper.Application.Features.Imports.Commands.RunImport;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

// Same split as PayrollRunExecutionTests: the command is dispatched directly, because the consumer is a
// thin adapter and broker delivery is covered elsewhere. What only a real database can answer is whether
// a batch really commits employees, balances, error rows and counters together.
public class ImportRunExecutionTests : IntegrationTestBase
{
    public ImportRunExecutionTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Header = "FirstName,LastName,Email,Department,Position,SalaryAmount,SalaryType,HireDate\n";

    private static string ValidRow(int i) =>
        $"First{i},Last{i},user{i}@import.test,Engineering,Developer,1000,Monthly,2024-01-15\n";

    private int BatchSize => Services.GetRequiredService<IOptions<ImportSettings>>().Value.BatchSize;

    private Task SeedReferencesAsync() =>
        Task.WhenAll(
            SeedDepartmentAsync("Engineering"),
            SeedPositionAsync("Developer", EmployeeGrade.Junior));

    private async Task<Guid> UploadAsync(string csv)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");

        var resp = await Client.PostAsync("v1/imports", new MultipartFormDataContent { { file, "file", "employees.csv" } });
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        return (await resp.Content.ReadFromJsonAsync<ImportJobResponse>(Json))!.Id;
    }

    private async Task RunImportAsync(Guid jobId)
    {
        using var scope = Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new RunImportCommand(jobId));
    }

    [Fact]
    public async Task RunImport_WithValidFile_HiresEveryRow_AndCompletesJob()
    {
        // ARRANGE
        await SeedReferencesAsync();
        var jobId = await UploadAsync(Header +
                                      "Aigerim,Nurlanova,Aigerim.N@Import.Test,Engineering,Developer,1500.50,Monthly,2024-01-15\n" +
                                      ValidRow(2) +
                                      ValidRow(3));

        // ACT
        await RunImportAsync(jobId);

        // ASSERT
        await WithDbContextAsync(async db =>
        {
            var job = await db.ImportJobs.SingleAsync(j => j.Id == jobId);
            job.Status.Should().Be(ImportJobStatus.Completed);
            job.TotalRows.Should().Be(3);
            job.ImportedCount.Should().Be(3);
            job.FailedCount.Should().Be(0);
            job.StartedAt.Should().NotBeNull();
            job.CompletedAt.Should().NotBeNull();

            (await db.ImportErrors.CountAsync(e => e.ImportJobId == jobId)).Should().Be(0);

            // Normalized by the factory, so the import and the unique index agree on what a duplicate is.
            var employee = await db.Employees.SingleAsync(e => e.Email == "aigerim.n@import.test");
            employee.Status.Should().Be(EmployeeStatus.Active);

            var salary = await db.Salaries.SingleAsync(s => s.EmployeeId == employee.Id);
            salary.Amount.Should().Be(1500.50m);
            salary.Type.Should().Be(SalaryType.Monthly);
            salary.EndDate.Should().BeNull();

            // The gap EmployeeFactory exists to close: an imported employee without leave balances would
            // only surface later, in the leave feature.
            (await db.LeaveBalances.CountAsync(b => b.EmployeeId == employee.Id))
                .Should().Be(Enum.GetValues<LeaveType>().Length);
        });
    }

    [Fact]
    public async Task RunImport_WithFileLargerThanOneBatch_ImportsEveryBatch()
    {
        // ARRANGE
        // One row past a batch boundary: the second batch runs after ClearTracking, which is exactly where
        // a progress update holding a stale tracked job, or a lost outbox row, would show up.
        await SeedReferencesAsync();
        var rows = BatchSize + 1;
        var jobId = await UploadAsync(Header + string.Concat(Enumerable.Range(0, rows).Select(ValidRow)));

        // ACT
        await RunImportAsync(jobId);

        // ASSERT
        await WithDbContextAsync(async db =>
        {
            var job = await db.ImportJobs.SingleAsync(j => j.Id == jobId);
            job.Status.Should().Be(ImportJobStatus.Completed);
            job.TotalRows.Should().Be(rows);
            job.ImportedCount.Should().Be(rows);

            (await db.Employees.CountAsync(e => e.Email.EndsWith("@import.test"))).Should().Be(rows);
        });
    }

    [Fact]
    public async Task RunImport_WithBadRows_ReportsEachByLine_AndImportsTheRest()
    {
        // ARRANGE
        await SeedReferencesAsync();
        var jobId = await UploadAsync(Header +
                                      ValidRow(1) +                                                                   // line 2
                                      "Bad,Department,bad.dep@import.test,Marketing,Developer,1000,Monthly,2024-01-15\n" + // line 3
                                      ValidRow(3) +                                                                   // line 4
                                      "Bad,Everything,bad.all@import.test,Marketing,Astronaut,1000,50,Yearly,soon\n");    // line 5

        // ACT
        await RunImportAsync(jobId);

        // ASSERT
        await WithDbContextAsync(async db =>
        {
            var job = await db.ImportJobs.SingleAsync(j => j.Id == jobId);
            job.Status.Should().Be(ImportJobStatus.Completed);
            job.ImportedCount.Should().Be(2);
            job.FailedCount.Should().Be(2);
            // The invariant a completed job owes.
            (job.ImportedCount + job.FailedCount).Should().Be(job.TotalRows);

            var errors = await db.ImportErrors
                .Where(e => e.ImportJobId == jobId)
                .OrderBy(e => e.RowNumber)
                .ToListAsync();

            // One error row per failed file row, numbered by the line an editor shows — however many rules
            // the row broke.
            errors.Select(e => e.RowNumber).Should().Equal(3, 5);
            errors[0].Message.Should().Contain("Unknown department 'Marketing'");
            errors[1].Message.Should().Contain("Unknown department").And.Contain("Unknown position");

            (await db.Employees.AnyAsync(e => e.Email.StartsWith("bad."))).Should().BeFalse();
        });
    }

    [Fact]
    public async Task RunImport_WhenJobAlreadyCompleted_DoesNothingOnRedelivery()
    {
        // ARRANGE
        await SeedReferencesAsync();
        var jobId = await UploadAsync(Header + ValidRow(1) + ValidRow(2));
        await RunImportAsync(jobId);

        var completedAt = await WithDbContextAsync(async db =>
            (await db.ImportJobs.SingleAsync(j => j.Id == jobId)).CompletedAt);

        // ACT
        // Without the fast path this would walk the file again and hit the unique email index on row one.
        var act = () => RunImportAsync(jobId);

        // ASSERT
        await act.Should().NotThrowAsync();

        await WithDbContextAsync(async db =>
        {
            var job = await db.ImportJobs.SingleAsync(j => j.Id == jobId);
            job.ImportedCount.Should().Be(2);
            job.CompletedAt.Should().Be(completedAt);

            (await db.Employees.CountAsync(e => e.Email.EndsWith("@import.test"))).Should().Be(2);
        });
    }

    [Fact]
    public async Task RunImport_WhenJobDoesNotExist_ThrowsNotFound()
    {
        // ACT
        var act = () => RunImportAsync(Guid.NewGuid());

        // ASSERT
        // NotFoundException is IPermanentFailure: the consumer sends it straight to the error queue, no retries.
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
