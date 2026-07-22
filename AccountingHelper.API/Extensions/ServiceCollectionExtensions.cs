using System.Text.Json.Serialization;
using AccountingHelper.API.Filters;
using AccountingHelper.API.Middleware;
using AccountingHelper.Domain.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace AccountingHelper.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        
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

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "postgresql",
                tags: ["database", "postgres", "live"]);
        
        services.Configure<MvcOptions>(o => o.Filters.Add<ValidationFilter>());
        
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