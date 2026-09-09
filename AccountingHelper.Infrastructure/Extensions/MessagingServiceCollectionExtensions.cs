using AccountingHelper.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AccountingHelper.Infrastructure.Extensions;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessagingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection(RabbitMqSettings.SectionName))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Host), "RabbitMq:Host is required")
            .Validate(s => !string.IsNullOrWhiteSpace(s.UserName), "RabbitMq:UserName is required")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Password), "RabbitMq:Password is required")
            .Validate(s => s.Port > 0, "RabbitMq:Port must be positive")
            .ValidateOnStart();
            
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            
            configureConsumers?.Invoke(x);
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
                cfg.Host(rabbitSettings.Host, rabbitSettings.Port, rabbitSettings.VirtualHost, h =>
                {
                    h.Username(rabbitSettings.UserName);
                    h.Password(rabbitSettings.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
}