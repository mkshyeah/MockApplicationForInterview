using AccountingHelper.Worker.Consumers;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.UnitTests.Worker;

// Writing a consumer and forgetting to register it costs nothing at compile time and nothing at
// startup: no queue is bound, the broker drops the message at the exchange, and the work silently
// never happens. That is how PayrollRunConsumer sat unwired through a whole block.
public class WorkerConsumerRegistrationTests
{
    [Fact]
    public void EveryConsumerInTheWorkerAssembly_IsRegistered()
    {
        // ARRANGE
        var declared = typeof(WorkerConsumers).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)))
            .ToList();

        declared.Should().NotBeEmpty("the assembly is supposed to contain consumers");

        // ACT
        // Registered against the in-memory transport: the question is what ends up in the container,
        // which has nothing to do with the broker.
        var services = new ServiceCollection();

        services.AddMassTransit(x =>
        {
            WorkerConsumers.Register(x);
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        // ASSERT
        var registered = services.Select(d => d.ServiceType).ToHashSet();

        declared.Should().OnlyContain(
            consumer => registered.Contains(consumer),
            "a consumer missing from WorkerConsumers.Register never gets a queue");
    }
}
