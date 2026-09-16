using MassTransit;

namespace AccountingHelper.Worker.Consumers;

// The single list of what this host consumes. It exists so a test can assert against the same
// list the host uses — registering a consumer is one line that nothing else verifies, and the
// failure mode is silent: no queue is bound, the broker drops the message, and the work never
// happens.
public static class WorkerConsumers
{
    public static void Register(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<PingConsumer>();
        configurator.AddConsumer<CacheInvalidationConsumer>();
        configurator.AddConsumer<PayrollRunConsumer>();
    }
}
