using System.Globalization;
using System.Text.Json.Serialization;
using AccountingHelper.API.Middleware;
using AccountingHelper.Domain.Interfaces;
using Asp.Versioning;
using FluentValidation;

namespace AccountingHelper.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en-US");
        
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
        
        return services;
    }

    public static IConfigurationBuilder AddCustomConfiguration(
        this IConfigurationBuilder configuration,
        IWebHostEnvironment environment)
    {
        configuration
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        
        if (environment.IsEnvironment("local"))
        {
            configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
            
            configuration.AddUserSecrets<Program>();
        }
        
        configuration.AddEnvironmentVariables();

        return configuration;
    }
    
}