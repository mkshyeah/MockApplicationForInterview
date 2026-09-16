namespace AccountingHelper.Infrastructure.Messaging;

public sealed class OutboxSettings
{
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(1);
}