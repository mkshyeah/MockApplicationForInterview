using System.Text.Json.Serialization;
using AccountingHelper.API.Middleware;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Application.Services;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Data.Repositories;
using AccountingHelper.Infrastructure.UnitOfWork;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ISalaryRepository, SalaryRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();

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

        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        return services;
    }

    public static void AddCustomConfiguration(this IConfigurationBuilder configuration,
        IWebHostEnvironment environment)
    {
        configuration
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        
        if (environment.IsEnvironment("local"))
        {
            configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
        }
        
        configuration.AddEnvironmentVariables();
    }
}