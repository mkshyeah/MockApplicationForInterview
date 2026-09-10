using AccountingHelper.Contracts;
using AccountingHelper.Contracts.Messaging;
using MassTransit;

namespace AccountingHelper.Worker.Consumers;

public sealed class PingConsumer : IConsumer<PingRequested>
{
    private readonly ILogger<PingConsumer> _logger;
    
    public PingConsumer(ILogger<PingConsumer> logger)
    {
        _logger = logger;
    }
    
    public Task Consume(ConsumeContext<PingRequested> context)
    {
        _logger.LogInformation("Ping received: {PingId}", context.Message.PingId);
        return Task.CompletedTask;
    }
}