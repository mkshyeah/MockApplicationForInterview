using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Messaging;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

            x.ConfigureHealthCheckOptions(options =>
            {
                options.Name = "rabbitmq-bus";
                options.MinimalFailureStatus = HealthStatus.Unhealthy;
                options.Tags.Add("broker");
                options.Tags.Add("ready");
            });
            
            configureConsumers?.Invoke(x);
            
            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.UsePostgres();
                // The half that matters here: it makes the scoped IPublishEndpoint write into the outbox
                // table inside the caller's transaction instead of talking to RabbitMQ.
                o.UseBusOutbox();
                
                // The delivery service polls; the default is tuned for production, and every test that waits
                // for an event would pay it. One second keeps the tests honest and costs nothing on a table 
                // that is empty almost always.
                o.QueryDelay = TimeSpan.FromSeconds(1);
            });
            
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