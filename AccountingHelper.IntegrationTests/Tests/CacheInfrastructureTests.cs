using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
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

    [Fact]
    public async Task HybridCache_WhenRedisUnavailable_StillReturnsValueFromFactory()
    {
        // ARRANGE
        // the test environment points at an unreachable Redis (see CustomWebApplicationFactory),
        // so every call has to fall through the dead L2 down to the factory
        var cache = Services.GetRequiredService<HybridCache>();
        var key = $"cache-infrastructure-test:{Guid.NewGuid():N}";

        // ACT
        var value = await cache.GetOrCreateAsync(
            key,
            _ => ValueTask.FromResult("computed-by-factory"));

        // ASSERT
        // fail-open at the cache level: a dead L2 must not surface as an exception
        value.Should().Be("computed-by-factory");
    }
}
