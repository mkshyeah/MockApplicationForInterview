using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Data.Repositories;
using AccountingHelper.Infrastructure.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            options.UseSnakeCaseNamingConvention();
        });
        
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(
                name: "database_context",
                tags: ["database", "efcore", "live"]);
        
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<ISalaryRepository, SalaryRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddTransient<CorrelationIdDelegatingHandler>();
        
        return services;
    }
}