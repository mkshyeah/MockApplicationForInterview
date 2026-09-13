using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class PayrollRunCreateTests : IntegrationTestBase
{
    public PayrollRunCreateTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string Route = "v1/payroll-runs";

    // Last month rather than a literal: the validator rejects future periods, so a hardcoded year
    // would start failing the day it arrives. Until time is injectable, the test has to read the
    // same clock the validator does.
    private static readonly DateTime Period = DateTime.UtcNow.AddMonths(-1);

    [Fact]
    public async Task CreatePayrollRun_Returns202_WithLocation_AndStoresPendingRun()
    {
        // ACT
        var resp = await Client.PostAsJsonAsync(Route, new { year = Period.Year, month = Period.Month });

        // ASSERT
        // 202, not 201: the row exists but the work has not been done, and the client is expected
        // to poll. A 201 here would promise a finished payroll run.
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await resp.Content.ReadFromJsonAsync<PayrollRunResponse>(Json);
        body!.Year.Should().Be(Period.Year);
        body.Month.Should().Be(Period.Month);
        body.Status.Should().Be(PayrollRunStatus.Pending);

        // Following the header is the assertion that matters — comparing the URL as a string would
        // pass just as well on a Location nobody can actually resolve.
        resp.Headers.Location.Should().NotBeNull();

        var followed = await Client.GetAsync(resp.Headers.Location);
        followed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await followed.Content.ReadFromJsonAsync<PayrollRunResponse>(Json))!.Id.Should().Be(body.Id);

        // The run is persisted as Pending with nothing processed: no consumer exists yet, so any
        // other state would mean someone moved it without doing the work.
        await WithDbContextAsync(async db =>
        {
            var run = await db.PayrollRuns.SingleAsync();

            run.Id.Should().Be(body.Id);
            run.Year.Should().Be(Period.Year);
            run.Month.Should().Be(Period.Month);
            run.Status.Should().Be(PayrollRunStatus.Pending);
            run.ProcessedCount.Should().Be(0);
            run.SkippedCount.Should().Be(0);
            run.StartedAt.Should().BeNull();
            run.CompletedAt.Should().BeNull();
        });
    }

    [Fact]
    public async Task CreatePayrollRun_ForSamePeriodTwice_Returns409_AndDoesNotCreateSecondRun()
    {
        // ACT
        var first = await Client.PostAsJsonAsync(Route, new { year = Period.Year, month = Period.Month });
        var second = await Client.PostAsJsonAsync(Route, new { year = Period.Year, month = Period.Month });

        // ASSERT
        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // SingleAsync is the assertion: it throws on zero rows and on two, so "exactly one run
        // exists" needs nothing else. The run's own fields belong to the test that creates one.
        await WithDbContextAsync(async db => await db.PayrollRuns.SingleAsync());
    }

    [Fact]
    public async Task CreatePayrollRun_WithMonthOutOfRange_Returns400()
    {
        // ACT
        var resp = await Client.PostAsJsonAsync(Route, new { year = Period.Year, month = 13 });

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Title.Should().Be("Validation Failed");
        problem.Extensions.Should().ContainKey("correlationId");

        // Only the month rule fires. The period rule short-circuits on an invalid month on purpose:
        // without that guard it would either throw or add a second, confusing error about a period
        // that was never valid to begin with.
        problem.Errors.Should().ContainSingle();
        problem.Errors.Should().ContainKey("month");

        await WithDbContextAsync(async db => (await db.PayrollRuns.CountAsync()).Should().Be(0));
    }

    [Fact]
    public async Task CreatePayrollRun_ForFuturePeriod_Returns400()
    {
        // ARRANGE
        var future = DateTime.UtcNow.AddMonths(1);

        // ACT
        var resp = await Client.PostAsJsonAsync(Route, new { year = future.Year, month = future.Month });

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        problem!.Errors.Should().ContainKey("period");

        await WithDbContextAsync(async db => (await db.PayrollRuns.CountAsync()).Should().Be(0));
    }

    [Fact]
    public async Task GetPayrollRun_WhenRunDoesNotExist_Returns404()
    {
        // ACT
        var resp = await Client.GetAsync($"{Route}/{Guid.NewGuid()}");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        problem!.Extensions.Should().ContainKey("correlationId");
    }
}
