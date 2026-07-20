using System.Net;
using System.Net.Http.Json;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ApplicationHealthTests : IntegrationTestBase
{
    private sealed record HealthCheckEntry(string Name, string Status);
 
    private sealed record HealthReportDto(string Status, List<HealthCheckEntry> Checks);
    public ApplicationHealthTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHealthLive_WhenDatabaseReachable_Returns200AndHealthy()
    {
        //ARRANGE
 
        //ACT
        var resp = await Client.GetAsync("/health/live");
 
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
 
        var report = await resp.Content.ReadFromJsonAsync<HealthReportDto>(Json);
 
        report!.Status.Should().Be("Healthy");
        report.Checks.Should().NotBeEmpty();
        report.Checks.Should().OnlyContain(c => c.Status == "Healthy");
        report.Checks.Should().Contain(c => c.Name == "postgresql");
    }
}