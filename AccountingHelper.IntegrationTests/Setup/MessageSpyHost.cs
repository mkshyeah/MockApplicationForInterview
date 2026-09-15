using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.IntegrationTests.Setup;

public class MessageSpyHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IBusControl _bus;
    
    public ITestHarness Harness { get; }

    private MessageSpyHost(
        ServiceProvider provider, 
        IBusControl bus,
        ITestHarness harness)
    {
        _provider = provider;
        _bus = bus;
        Harness = harness;
    }

    public static async Task<MessageSpyHost> StartAsync(string rabbitMqConnectionString)
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddMassTransitTestHarness(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<SalaryChangedSpyConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitMqConnectionString));
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        
        return new MessageSpyHost(provider, provider.GetRequiredService<IBusControl>(), harness);
    }
    
    public async ValueTask DisposeAsync()
    {
        await _bus.StopAsync();
        await _provider.DisposeAsync();
    }
}