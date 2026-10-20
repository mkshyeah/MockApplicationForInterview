namespace AccountingHelper.Infrastructure.Messaging;

public sealed class RetrySettings
{
    // One policy, not a composition: two zero-length intervals are the immediate attempts,
    // the rest are the spaced ones. Calling Immediate() and Interval() separately does not
    // stack — the second call replaces the first, and the attempt count quietly halves.
    //
    // No default on purpose. ConfigurationBinder appends configured elements to an array that already has
    // items, so a default here doubled the policy of every host that configured one, and silently stood in
    // for it in every host that did not. Empty fails ValidateOnStart instead.
    public TimeSpan[] Intervals { get; set; } = [];
}
