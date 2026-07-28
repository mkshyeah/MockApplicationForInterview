using System.Globalization;
using AccountingHelper.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.Application.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            }
            );
        
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("en-US");
        
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}