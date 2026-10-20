using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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
    // Published so ResetAsync can list and purge queues over HTTP: AMQP can purge a queue it knows by name
    // but cannot enumerate them, and a hardcoded list rots with the next consumer.
    private const int ManagementPort = 15672;

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithPortBinding(ManagementPort, true)
        .Build();

    private Respawner _respawner = null!;
    private DbConnection _connection = null!;
    private HttpClient _rabbitManagement = null!;

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

        _rabbitManagement = CreateRabbitManagementClient();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            // The outbox tables belong to MassTransit, not to the application, and its delivery
            // service writes to them on a timer. Truncating them while it holds row locks is a
            // deadlock between two writers — Postgres kills one, and the victim is usually
            // Respawn, which fails whichever test happened to start at that moment.
            TablesToIgnore = new Respawn.Graph.Table[]
            {
                "__EFMigrationsHistory",
                "outbox_message",
                "outbox_state",
                "inbox_state"
            }
        });
    }

    public async Task ResetAsync()
    {
        // Order matters: the outbox first, so nothing is delivered after the queues are purged.
        await ClearOutboxAsync();
        await PurgeQueuesAsync();

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

    // Why this cannot race the delivery service, checked against the SQL it actually runs: one delivery is
    // one transaction that locks its outbox_state row (FOR UPDATE SKIP LOCKED), reads the messages without
    // a lock, sends them, deletes them and commits. The messages alone are not a synchronisation point —
    // a DELETE of outbox_message by itself commits in the middle of a send, and that message then lands in
    // a queue after the purge. Deleting outbox_state in the same transaction is what makes this wait for
    // any delivery in flight, so the two statements must stay together.
    //
    // Respawn's TRUNCATE deadlocked against the same locks. Here a short lock_timeout gives up before
    // Postgres starts deadlock detection (deadlock_timeout, 1 s by default), so the delivery service always
    // wins and this side retries. Whatever it shipped meanwhile is in a queue by then, and the purge that
    // follows removes it.
    private async Task ClearOutboxAsync()
    {
        for (var attempt = 1;; attempt++)
        {
            await using var transaction = await _connection.BeginTransactionAsync();

            try
            {
                await using var command = _connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText =
                    "SET LOCAL lock_timeout = '100ms'; DELETE FROM outbox_message; DELETE FROM outbox_state;";

                await command.ExecuteNonQueryAsync();
                await transaction.CommitAsync();
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.LockNotAvailable && attempt < 50)
            {
                await transaction.RollbackAsync();
            }
        }
    }

    // Every queue in the vhost, error and skipped queues included: a consumer started by a later test must
    // not find work left by an earlier one. Purging keeps the queues and their bindings — the topology is
    // declared once per run, only the messages are per test.
    private async Task PurgeQueuesAsync()
    {
        var queues = await _rabbitManagement.GetFromJsonAsync<List<RabbitQueue>>("api/queues/%2F?columns=name");

        foreach (var queue in queues!)
        {
            var resp = await _rabbitManagement.DeleteAsync(
                $"api/queues/%2F/{Uri.EscapeDataString(queue.Name)}/contents");
            resp.EnsureSuccessStatusCode();
        }
    }

    private HttpClient CreateRabbitManagementClient()
    {
        // Credentials come from the AMQP connection string, so they cannot drift from what the container uses.
        var userInfo = new Uri(_rabbitMq.GetConnectionString()).UserInfo;

        return new HttpClient
        {
            BaseAddress = new Uri($"http://{_rabbitMq.Hostname}:{_rabbitMq.GetMappedPublicPort(ManagementPort)}/"),
            // A paused broker answers nothing. Fail the reset in seconds, not after HttpClient's default 100 s,
            // so a test that forgot ResumeBrokerAsync shows up as itself rather than as a slow suite.
            Timeout = TimeSpan.FromSeconds(10),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(userInfo))))
            }
        };
    }

    private sealed record RabbitQueue(string Name);

    // docker pause, not stop: pausing freezes the processes but keeps the container and its port
    // mapping, so the same connection string works again after resuming. Stop/Start would hand out
    // a different host port, and every host built with the old string would be pointing at nothing.
    public Task PauseBrokerAsync() => _rabbitMq.PauseAsync();
    public Task ResumeBrokerAsync() => _rabbitMq.UnpauseAsync();

    public async Task DisposeAsync()
    {
        _rabbitManagement.Dispose();
        await _connection.DisposeAsync();
        Factory.Dispose();
        await Task.WhenAll(
            _postgres.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _rabbitMq.DisposeAsync().AsTask());
    }
}