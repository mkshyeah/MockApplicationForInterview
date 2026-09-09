using AccountingHelper.API.Middleware;
using AccountingHelper.API.Serialization;
using AccountingHelper.Application.Caching;
using AccountingHelper.Domain.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AccountingHelper.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        
        var redisConnectionString =  configuration["Redis:ConnectionString"]                                                                                                                                                          
                                     ?? throw new InvalidOperationException("Redis connection string 'ConnectionString' not found."); 

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new StrictEnumConverterFactory()))
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
        
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = configuration["Redis:InstanceName"];
        });

        services.AddHybridCache();
        
        services.AddOptions<CacheSettings>()
            .Bind(configuration.GetSection(CacheSettings.SectionName))
            .Validate(s => s.Dashboard > TimeSpan.Zero, "Cache:Dashboard must be positive")
            .Validate(s => s.Lookups > TimeSpan.Zero, "Cache:Lookups must be positive")
            .ValidateOnStart();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "postgresql",
                tags: ["database", "postgres", "live", "ready"])
            .AddRedis(
                redisConnectionString: redisConnectionString,
                name: "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["cache", "redis", "live", "ready"]);
        
        
        return services;
    }

    public static IConfigurationBuilder AddCustomConfiguration(
        this IConfigurationBuilder configuration,
        IWebHostEnvironment environment)
    {
        if (environment.IsEnvironment("local"))
        {
            configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
            configuration.AddUserSecrets<Program>();
        }

        return configuration;
    }
    
}