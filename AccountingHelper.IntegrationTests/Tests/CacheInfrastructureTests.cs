using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class CacheInfrastructureTests : IntegrationTestBase
{
    public CacheInfrastructureTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void HybridCache_IsResolvableFromContainer()
    {
        // ARRANGE

        // ACT
        var cache = Services.GetService<HybridCache>();

        // ASSERT
        cache.Should().NotBeNull();
    }

    // Страж конфигурации, а не поведения. Префикс ключей приходит из базового
    // appsettings.json; если он однажды переедет обратно в appsettings.local.json,
    // тесты начнут писать ключи без префикса и проверять не ту схему именования,
    // что работает в проде, — молча и без единого красного теста.
    [Fact]
    public void RedisInstanceName_ComesFromBaseAppSettings()
    {
        // ARRANGE
        var configuration = Services.GetRequiredService<IConfiguration>();

        // ACT
        var instanceName = configuration["Redis:InstanceName"];

        // ASSERT
        instanceName.Should().Be("accounting-helper:");
    }

    // Кэш живой и повторное чтение идёт не через фабрику. Проверяем наблюдаемое поведение
    // (второй вызов не дёргает фабрику), а не содержимое Redis: запись в L2 у HybridCache
    // fire-and-forget, ключ появляется позже ответа, и прямая проверка мигала бы.
    [Fact]
    public async Task HybridCache_WhenRedisReachable_ServesSecondCallWithoutInvokingFactory()
    {
        // ARRANGE
        var cache = Services.GetRequiredService<HybridCache>();
        var key = $"cache-infrastructure-test:{Guid.NewGuid():N}";
        var factoryCalls = 0;

        ValueTask<string> Factory(CancellationToken _)
        {
            factoryCalls++;
            return ValueTask.FromResult("computed-by-factory");
        }

        // ACT
        var first = await cache.GetOrCreateAsync(key, Factory);
        var second = await cache.GetOrCreateAsync(key, Factory);

        // ASSERT
        first.Should().Be("computed-by-factory");
        second.Should().Be("computed-by-factory");
        factoryCalls.Should().Be(1);

        // ключ временный и в CacheKeys не входит — убираем за собой сами,
        // иначе он переживёт ResetAsync, который ходит только по каталогу
        await cache.RemoveAsync(key);
    }
}
