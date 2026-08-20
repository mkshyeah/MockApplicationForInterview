using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

// Второе окружение: живая база, заведомо мёртвый кэш. Нужно ровно для того, чтобы
// политика fail-open осталась исполняемой спецификацией после переезда основного
// набора на живой Redis. Данных не пишет и Respawn не требует — тестам здесь нужен
// только поднятый хост.
public class DeadRedisFixture : IAsyncLifetime
{
    // Свой Postgres, а не общий с IntegrationFixture: health-check обязан видеть живую базу
    // (иначе агрегат уедет в Unhealthy и придёт 503, а тест — про 200 и Degraded).
    // Разделить контейнер между двумя коллекционными фикстурами можно только через статику,
    // и это ровно та связь через общее состояние, которой мы избегаем. Лишний контейнер дешевле.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public CustomWebApplicationFactory Factory { get; private set; } = null!;
    public HttpClient CreateClient() => Factory.CreateClient();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new CustomWebApplicationFactory(_postgres.GetConnectionString(), RedisEndpoints.Dead);

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
