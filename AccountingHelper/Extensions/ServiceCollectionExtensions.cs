using System.Text.Json.Serialization;
using AccountingHelper.Contexts;
using AccountingHelper.Middleware;
using AccountingHelper.Services;
using AccountingHelper.Services.Interfaces;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Настройка БД
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        // 2. Регистрация бизнес-логики
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IReportService,ReportService>();
        
        // 3. Добавляем версионирование
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                // Это как раз то, что лечит ошибку: формат версии в пути
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        
        // 4. Настройка контроллеров и JSON
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Чтобы Enum в JSON были строками, а не цифрами
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        
        // 5. Маленькие буквы в URL
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        // 6. Глобальная обработка ошибок
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