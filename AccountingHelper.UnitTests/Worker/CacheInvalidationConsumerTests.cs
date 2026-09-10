using AccountingHelper.Application.Caching;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AccountingHelper.UnitTests.Worker;

// Verify is the whole point here, unlike the IPublishEndpoint mocks in the command handlers:
// calling RemoveAsync with the right key IS the consumer's entire behaviour. There is no return
// value and no state to inspect, so asserting anything else would be asserting nothing.
public class CacheInvalidationConsumerTests
{
    private readonly Mock<HybridCache> _cacheMock = new();
    private readonly CacheInvalidationConsumer _consumer;

    public CacheInvalidationConsumerTests()
    {
        _consumer = new CacheInvalidationConsumer(
            _cacheMock.Object,
            NullLogger<CacheInvalidationConsumer>.Instance);
    }

    private static ConsumeContext<T> ContextFor<T>(T message) where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(c => c.Message).Returns(message);
        return context.Object;
    }

    private void VerifyDashboardEvictedAlone()
    {
        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.ReportingDashboard, It.IsAny<CancellationToken>()),
            Times.Once);

        // The lookups are not touched by employee mutations. Without these two the consumer could
        // quietly evict CacheKeys.All and every other test would stay green, while the one-hour
        // TTL on the reference lists silently became "until the next hire".
        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.LookupsDepartments, It.IsAny<CancellationToken>()),
            Times.Never);
        _cacheMock.Verify(
            c => c.RemoveAsync(CacheKeys.LookupsPositions, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_SalaryChanged_EvictsDashboardOnly()
    {
        // ARRANGE
        var context = ContextFor(new SalaryChanged(Guid.NewGuid(), Guid.NewGuid()));

        // ACT
        await _consumer.Consume(context);

        // ASSERT
        VerifyDashboardEvictedAlone();
    }

    [Fact]
    public async Task Consume_EmployeeHired_EvictsDashboardOnly()
    {
        // ARRANGE
        var context = ContextFor(new EmployeeHired(Guid.NewGuid()));

        // ACT
        await _consumer.Consume(context);

        // ASSERT
        VerifyDashboardEvictedAlone();
    }

    [Fact]
    public async Task Consume_EmployeeFired_EvictsDashboardOnly()
    {
        // ARRANGE
        var context = ContextFor(new EmployeeFired(Guid.NewGuid()));

        // ACT
        await _consumer.Consume(context);

        // ASSERT
        VerifyDashboardEvictedAlone();
    }
}
