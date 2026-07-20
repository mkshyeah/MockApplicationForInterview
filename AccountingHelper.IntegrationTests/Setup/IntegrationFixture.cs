using System.Data.Common;
using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

public class IntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private Respawner _respawner = null!;
    private DbConnection _connection = null!;

    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Factory = new CustomWebApplicationFactory(_container.GetConnectionString());

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
        }

        _connection = new NpgsqlConnection(_container.GetConnectionString());
        await _connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Respawn.Graph.Table[] { "__EFMigrationsHistory" }
        });
    }

    public Task ResetAsync() => _respawner.ResetAsync(_connection);

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        Factory.Dispose();
        await _container.DisposeAsync();
    }
}