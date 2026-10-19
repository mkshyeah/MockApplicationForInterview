using AccountingHelper.API.Middleware;
using AccountingHelper.API.Serialization;
using AccountingHelper.Application.Caching;
using AccountingHelper.Application.Features.Imports;
using AccountingHelper.Domain.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AccountingHelper.API.Extensions;

public static class ServiceCollectionExtensions
{
    private const long MultipartOverheadBytes = 64 * 1024;
    private const long KestrelMaxRequestBodySize = 30_000_000;

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
        
        services.AddOptions<ImportSettings>()
            .Bind(configuration.GetSection(ImportSettings.SectionName))
            .Validate(s => s.MaxFileSizeBytes > 0, "Import:MaxFileSizeBytes must be positive")
            .Validate(s => s.MaxFileSizeBytes + MultipartOverheadBytes <= KestrelMaxRequestBodySize,
                "Import:MaxFileSizeBytes plus multipart overhead must fit under Kestrel's request body limit")
            .Validate(s => s.BatchSize > 0, "Import:BatchSize must be positive")
            .ValidateOnStart();

        // Headroom above the file limit leaves room for multipart boundaries and part headers, so a file
        // slightly over the limit still reaches the validator and gets a 400 instead of a cut-off read.
        services.AddOptions<FormOptions>()
            .Configure<IOptions<ImportSettings>>((form, import) =>
                form.MultipartBodyLengthLimit = import.Value.MaxFileSizeBytes + MultipartOverheadBytes);

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