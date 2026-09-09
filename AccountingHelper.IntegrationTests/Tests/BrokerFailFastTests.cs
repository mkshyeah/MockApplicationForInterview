using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

[Collection("Integration:DeadBroker")]
public class BrokerFailFastTests
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _json;
    private readonly IServiceProvider _services;

    public BrokerFailFastTests(DeadBrokerFixture fixture)
    {
        _client = fixture.CreateClient();
        _services = fixture.Factory.Services;
        _json = _services.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
    }

    [Fact]
    public async Task GetHealthReady_WhenBrokerUnavailable_Returns503()
    {
        var resp = await _client.GetAsync("/health/ready");

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        
        var report = await resp.Content.ReadFromJsonAsync<HealthReportDto>(_json);
        report!.Checks.Should().Contain(c => c.Name == "rabbitmq-bus" && c.Status == "Unhealthy");
        
        report!.Checks.Should().Contain(c => c.Name == "postgresql" && c.Status == "Healthy");                                                                                                                
        report.Checks.Should().Contain(c => c.Name == "redis" && c.Status == "Healthy");
    }
    
    [Fact]
    public async Task GetHealthLive_WhenBrokerUnavailable_Returns200()
    {
        var resp = await _client.GetAsync("/health/live");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}