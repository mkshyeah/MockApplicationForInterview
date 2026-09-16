using AccountingHelper.Domain.Exceptions;
using MassTransit;

namespace AccountingHelper.Infrastructure.Messaging;

// The one place the retry policy is expressed, so tests assert against what the hosts actually use
// instead of a copy that can drift. Same reason WorkerConsumers exists.
public static class RetryPolicy
{
    public static void Configure(IRetryConfigurator configurator, TimeSpan[] intervals)
    {
        // One policy, not a composition: Immediate() followed by Interval() replaces rather than
        // stacks, and the attempt count silently drops. Zero-length entries are the immediate ones.
        configurator.Intervals(intervals);

        // Retrying cannot fix bad input, a missing row or a broken rule — it only delays the trip
        // to the error queue by the length of the whole policy.
        configurator.Ignore<Exception>(ex => ex is IPermanentFailure);
    }
}
