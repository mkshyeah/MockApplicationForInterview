using AccountingHelper.Application.DTOs.Validators;
using AccountingHelper.Application.Interfaces;
using AccountingHelper.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISalaryService, SalaryService>();
        
        services.AddValidatorsFromAssemblyContaining<ChangeSalaryRequestValidator>();

        return services;
    }
}