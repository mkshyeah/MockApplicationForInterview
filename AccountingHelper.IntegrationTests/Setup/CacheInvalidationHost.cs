using AccountingHelper.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.IntegrationTests.Setup;

public class CacheInvalidationHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IBusControl _bus;

    private CacheInvalidationHost(ServiceProvider provider, IBusControl bus)
    {
        _provider = provider;
        _bus = bus;
    }

    public static async Task<CacheInvalidationHost> StartAsync(
        IConfiguration apiConfiguration,
        string rabbitMqConnectionString)
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddStackExchangeRedisCache(o =>
            {
                // Both values come from the API host's configuration rather than literals:
                // a diverging InstanceName would evict nothing and still look green.
                o.Configuration = apiConfiguration["Redis:ConnectionString"];
                o.InstanceName = apiConfiguration["Redis:InstanceName"];
            })
            .AddHybridCache().Services
            .AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();
                x.AddConsumer<CacheInvalidationConsumer>();
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(new Uri(rabbitMqConnectionString));
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();

        return new CacheInvalidationHost(provider, bus);
    }

    public async ValueTask DisposeAsync()
    {
        await _bus.StopAsync();
        await _provider.DisposeAsync();
    }
}