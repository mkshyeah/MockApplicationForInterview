using AccountingHelper.Application.Caching;
using AccountingHelper.Application.Extensions;
using AccountingHelper.Infrastructure.Extensions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(new ExpressionTemplate(
        "[{@t:HH:mm:ss} {@l:u3}] {@m}\n{@x}",
        theme: TemplateTheme.Code))
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting worker host...");

    var builder = Host.CreateApplicationBuilder(args);
    
    if (builder.Environment.IsEnvironment("local"))
    {
        builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddUserSecrets<Program>();
    }

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
                                ?? throw new InvalidOperationException(
                                    "Redis connection string 'ConnectionString' not found.");

    // Registered here rather than shared with the API on purpose: composition roots are
    // allowed to repeat themselves. Moving StackExchangeRedis into Application would put a
    // concrete cache provider into a layer that is supposed to know only HybridCache.
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = builder.Configuration["Redis:InstanceName"];
    });

    builder.Services.AddHybridCache();

    builder.Services.AddOptions<CacheSettings>()
        .Bind(builder.Configuration.GetSection(CacheSettings.SectionName))
        .Validate(s => s.Dashboard > TimeSpan.Zero, "Cache:Dashboard must be positive")
        .Validate(s => s.Lookups > TimeSpan.Zero, "Cache:Lookups must be positive")
        .ValidateOnStart();

    builder.Services
        .AddInfrastructureServices(builder.Configuration)
        .AddApplicationServices();

    builder.Services.AddMessagingServices(configuration: builder.Configuration);
    
    builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true
    }));

    using var host = builder.Build();

    await host.RunAsync();

    Log.Information("Stopped cleanly");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
