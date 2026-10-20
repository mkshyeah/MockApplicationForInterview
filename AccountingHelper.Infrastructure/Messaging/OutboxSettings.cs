namespace AccountingHelper.Infrastructure.Messaging;

public sealed class OutboxSettings
{
    // No default: a host without a RabbitMq:Outbox section used to poll at this value without saying so.
    // AddMessagingServices rejects zero, because this one value is read before ValidateOnStart can see it.
    public TimeSpan QueryDelay { get; set; }
}
