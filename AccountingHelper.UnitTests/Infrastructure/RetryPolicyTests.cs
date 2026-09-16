using AccountingHelper.Infrastructure.Messaging;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.UnitTests.Infrastructure;

// Retry counting runs on the in-memory transport: the question is how many times MassTransit calls
// the consumer, and that is transport-independent. Timings are collapsed to a millisecond so the
// shape of the policy — how many steps it has — is what gets asserted.
public class RetryPolicyTests
{
    private sealed record AlwaysFails;

    private sealed class AlwaysFailsConsumer : IConsumer<AlwaysFails>
    {
        public static int Attempts;

        public Task Consume(ConsumeContext<AlwaysFails> context)
        {
            Interlocked.Increment(ref Attempts);
            throw new InvalidOperationException("poison");
        }
    }

    [Fact]
    public async Task ConfiguredRetryPolicy_CallsTheConsumerOncePerIntervalPlusOne()
    {
        // ARRANGE
        AlwaysFailsConsumer.Attempts = 0;

        var steps = new RetrySettings().Intervals.Length;
        var intervals = Enumerable.Repeat(TimeSpan.FromMilliseconds(1), steps).ToArray();

        await using var provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<AlwaysFailsConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Intervals(intervals));
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // ACT
        await harness.Bus.Publish(new AlwaysFails());
        (await harness.Consumed.Any<AlwaysFails>()).Should().BeTrue();

        // ASSERT
        // One initial delivery plus one retry per configured interval.
        AlwaysFailsConsumer.Attempts.Should().Be(steps + 1);
    }
}
