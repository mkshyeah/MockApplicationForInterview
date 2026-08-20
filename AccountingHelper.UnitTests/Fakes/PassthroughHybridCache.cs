using Microsoft.Extensions.Caching.Hybrid;

namespace AccountingHelper.UnitTests.Fakes;

/// <summary>
/// A <see cref="HybridCache"/> that never caches: every read runs the factory.
/// Handlers take the cache as a dependency, but what a unit test has to pin down is the
/// arithmetic inside the factory — whether a second call skips it is cache behaviour, and
/// that is proven against a real Redis in the integration suite. Faking it here would only
/// re-test HybridCache itself.
/// </summary>
public sealed class PassthroughHybridCache : HybridCache
{
    public override ValueTask<T> GetOrCreateAsync<TState, T>(
        string key,
        TState state,
        Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) =>
        factory(state, cancellationToken);

    public override ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryOptions? options = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) => default;

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => default;

    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => default;
}
