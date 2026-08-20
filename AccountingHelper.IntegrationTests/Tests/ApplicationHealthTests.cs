using System.Net;
using System.Net.Http.Json;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ApplicationHealthTests : IntegrationTestBase
{
    public ApplicationHealthTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHealthLive_WhenDatabaseReachable_Returns200AndDatabaseChecksHealthy()
    {
        // ARRANGE

        // ACT
        var resp = await Client.GetAsync("/health/live");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await resp.Content.ReadFromJsonAsync<HealthReportDto>(Json);

        report!.Checks.Should().NotBeEmpty();
        // scoped to the database checks only: the aggregate status also reflects the cache,
        // which this test says nothing about
        report.Checks.Should().Contain(c => c.Name == "postgresql" && c.Status == "Healthy");
        report.Checks.Should().Contain(c => c.Name == "database_context" && c.Status == "Healthy");
    }

    // Зеркало теста про Degraded из CacheFailOpenTests: тот прогоняется на мёртвом Redis,
    // этот — на контейнерном живом. Обе ветки политики fail-open стали исполняемыми,
    // и "зелёный" перестал означать просто отсутствие красного.
    [Fact]
    public async Task GetHealthLive_WhenRedisReachable_Returns200AndRedisHealthy()
    {
        // ARRANGE

        // ACT
        var resp = await Client.GetAsync("/health/live");

        // ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var report = await resp.Content.ReadFromJsonAsync<HealthReportDto>(Json);

        report!.Checks.Should().Contain(c => c.Name == "redis" && c.Status == "Healthy");
        // с живым кэшем деградировать больше нечему: агрегат обязан быть полностью зелёным
        report.Status.Should().Be("Healthy");
    }
}
