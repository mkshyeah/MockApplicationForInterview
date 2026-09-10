using System.Data.Common;
using AccountingHelper.Application.Caching;
using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

public class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

    private Respawner _respawner = null!;
    private DbConnection _connection = null!;

    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient CreateClient() => Factory.CreateClient();
    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _rabbitMq.StartAsync());

        Factory = new CustomWebApplicationFactory(
            _postgres.GetConnectionString(),
            RedisEndpoints.Live(_redis.GetConnectionString()),
            _rabbitMq.GetConnectionString());
        
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        _connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
        });
    }

    public async Task ResetAsync()
    {
        await _respawner.ResetAsync(_connection);

        // FlushDatabase здесь был бы бесполезен: HybridCache держит L1 в памяти процесса,
        // а фабрика одна на весь прогон — значение из предыдущего теста переживает и Respawn,
        // и очистку Redis, и следующий тест получает чужое число при пустой базе.
        // RemoveAsync снимает запись с обоих уровней, поэтому обходим каталог целиком.
        // Кэш берём из Factory.Services: это тот же синглтон, что обслуживает запросы.
        var cache = Factory.Services.GetRequiredService<HybridCache>();

        foreach (var key in CacheKeys.All)
        {
            await cache.RemoveAsync(key);
        }
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        Factory.Dispose();
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _rabbitMq.DisposeAsync().AsTask());
    }
}