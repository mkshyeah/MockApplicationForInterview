using AccountingHelper.Application.Exceptions;
using AccountingHelper.Infrastructure.Messaging;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.UnitTests.Infrastructure;

// Retry counting runs on the in-memory transport: how many times MassTransit calls a consumer is
// transport-independent. Timings collapse to a millisecond, so what gets asserted is the shape of
// the policy — how many steps it has — through the same RetryPolicy the hosts configure.
public class RetryPolicyTests
{
    private sealed record AlwaysFails;
    private sealed record AlwaysRejects;

    private sealed class AlwaysFailsConsumer : IConsumer<AlwaysFails>
    {
        public static int Attempts;

        public Task Consume(ConsumeContext<AlwaysFails> context)
        {
            Interlocked.Increment(ref Attempts);
            throw new InvalidOperationException("transient");
        }
    }

    private sealed class AlwaysRejectsConsumer : IConsumer<AlwaysRejects>
    {
        public static int Attempts;

        public Task Consume(ConsumeContext<AlwaysRejects> context)
        {
            Interlocked.Increment(ref Attempts);

            // A permanent failure: NotFoundException inherits AccountingHelperException, which is
            // marked IPermanentFailure.
            throw new NotFoundException("PayrollRun", Guid.Empty);
        }
    }

    // The production shape — five steps, as in both hosts' appsettings.json — at a millisecond each. The count is
    // spelled out because RetrySettings has no default to borrow it from; MessagingSettingsTests checks the files.
    private static readonly TimeSpan[] Intervals = Enumerable.Repeat(TimeSpan.FromMilliseconds(1), 5).ToArray();

    private static async Task<ITestHarness> StartHarnessAsync(ServiceProvider provider)
    {
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        return harness;
    }

    private static ServiceProvider BuildProvider() =>
        new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<AlwaysFailsConsumer>();
                x.AddConsumer<AlwaysRejectsConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => RetryPolicy.Configure(r, Intervals));
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

    [Fact]
    public async Task TransientFailure_IsRetriedOncePerConfiguredInterval()
    {
        // ARRANGE
        AlwaysFailsConsumer.Attempts = 0;
        await using var provider = BuildProvider();
        var harness = await StartHarnessAsync(provider);

        // ACT
        await harness.Bus.Publish(new AlwaysFails());
        (await harness.Consumed.Any<AlwaysFails>()).Should().BeTrue();

        // ASSERT
        AlwaysFailsConsumer.Attempts.Should().Be(Intervals.Length + 1);
    }

    [Fact]
    public async Task PermanentFailure_IsNotRetried()
    {
        // ARRANGE
        AlwaysRejectsConsumer.Attempts = 0;
        await using var provider = BuildProvider();
        var harness = await StartHarnessAsync(provider);

        // ACT
        await harness.Bus.Publish(new AlwaysRejects());
        (await harness.Consumed.Any<AlwaysRejects>()).Should().BeTrue();

        // ASSERT
        // The answer will not change on the second try, so the message goes straight to the error
        // queue instead of holding the endpoint for the length of the whole policy.
        AlwaysRejectsConsumer.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExhaustedRetries_PublishFault()
    {
        // ARRANGE
        AlwaysFailsConsumer.Attempts = 0;
        await using var provider = BuildProvider();
        var harness = await StartHarnessAsync(provider);

        // ACT
        await harness.Bus.Publish(new AlwaysFails());

        // ASSERT
        // Fault<T> is the observable signal that a message gave up — it is what a monitor or a
        // compensating consumer subscribes to, unlike the error queue, which is a place to look.
        (await harness.Published.Any<Fault<AlwaysFails>>()).Should().BeTrue();
    }
}
