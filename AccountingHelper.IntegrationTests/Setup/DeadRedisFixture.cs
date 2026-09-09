using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

// Second environment: live database, deliberately dead cache. It exists so the fail-open
// policy stays an executable specification now that the main collection runs against a live
// Redis. It writes no data and needs no Respawn — these tests only need a running host.
public class DeadRedisFixture : IAsyncLifetime
{
    // Its own Postgres rather than the one from IntegrationFixture: the health check has to see
    // a live database, or the aggregate would go Unhealthy and answer 503 while the test is about
    // 200 and Degraded. Sharing a container between two collection fixtures is only possible
    // through statics, which is the coupling we are avoiding. An extra container is cheaper.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // The broker is irrelevant here, but ValidateOnStart demands a non-empty section. A dead
        // endpoint is safe: MassTransit retries in the background instead of blocking startup,
        // and the bus health check lives in readiness, which these tests never call.
        Factory = new CustomWebApplicationFactory(
            _postgres.GetConnectionString(),
            RedisEndpoints.Dead,
            RabbitMqEndpoints.Dead);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await _postgres.DisposeAsync();
    }
}
