namespace AccountingHelper.Infrastructure.Messaging;

public sealed class RetrySettings
{
    // One policy, not a composition: two zero-length intervals are the immediate attempts,
    // the rest are the spaced ones. Calling Immediate() and Interval() separately does not
    // stack — the second call replaces the first, and the attempt count quietly halves.
    public TimeSpan[] Intervals { get; set; } =
        [TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)];
}