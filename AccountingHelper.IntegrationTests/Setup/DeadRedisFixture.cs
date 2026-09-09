using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

public class DeadRedisFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new CustomWebApplicationFactory(_postgres.GetConnectionString(), RedisEndpoints.Dead, RabbitMqEndpoints.Dead);

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
