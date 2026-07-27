using System.Globalization;
using AccountingHelper.Application.DTOs.Validators;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;
        
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en-US");
        
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISalaryService, SalaryService>();
        
        services.AddValidatorsFromAssemblyContaining<ChangeSalaryRequestValidator>();

        return services;
    }
}