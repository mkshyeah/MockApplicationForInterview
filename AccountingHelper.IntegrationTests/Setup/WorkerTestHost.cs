using AccountingHelper.Application.Caching;
using AccountingHelper.Application.Extensions;
using AccountingHelper.Application.Features.Imports;
using AccountingHelper.Infrastructure.Extensions;
using AccountingHelper.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.IntegrationTests.Setup;

// The Worker process, minus the Generic Host: same containers, same configuration, and above all
// the same WorkerConsumers.Register the real host uses. Tests that dispatch commands through
// ISender never touch the wiring between a message and its consumer, and that wiring is exactly
// what broke once already.
public sealed class WorkerTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IBusControl _bus;

    private WorkerTestHost(ServiceProvider provider, IBusControl bus)
    {
        _provider = provider;
        _bus = bus;
    }

    public static async Task<WorkerTestHost> StartAsync(IConfiguration apiConfiguration)
    {
        // Borrowed whole rather than key by key: the API host already points at this run's
        // containers, and copying individual settings is how one of them gets forgotten.
        var configuration = new ConfigurationBuilder()
            .AddConfiguration(apiConfiguration)
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
            options.InstanceName = configuration["Redis:InstanceName"];
        });
        services.AddHybridCache();

        services.AddOptions<CacheSettings>()
            .Bind(configuration.GetSection(CacheSettings.SectionName));
        
        services.AddOptions<ImportSettings>()
            .Bind(configuration.GetSection(ImportSettings.SectionName));

        services
            .AddInfrastructureServices(configuration)
            .AddApplicationServices()
            .AddMessagingServices(configuration, WorkerConsumers.Register);

        var provider = services.BuildServiceProvider(validateScopes: true);

        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();

        return new WorkerTestHost(provider, bus);
    }

    public async ValueTask DisposeAsync()
    {
        await _bus.StopAsync();
        await _provider.DisposeAsync();
    }
}
