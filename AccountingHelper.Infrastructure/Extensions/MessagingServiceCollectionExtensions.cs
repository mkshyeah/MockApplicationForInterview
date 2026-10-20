using AccountingHelper.Domain.Exceptions;
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
            .Validate(s => s.Retry.Intervals.Length > 0, "RabbitMq:Retry:Intervals must not be empty")
            .Validate(s => s.Retry.Intervals.All(i => i >= TimeSpan.Zero), "RabbitMq:Retry:Intervals must not be negative")
            .Validate(s => s.Outbox.QueryDelay > TimeSpan.Zero, "RabbitMq:Outbox:QueryDelay must be positive")
            .Validate(s => s.Port > 0, "RabbitMq:Port must be positive")
            .ValidateOnStart();
        
        // AddEntityFrameworkOutbox wants a value while the container is still being built, so this one
        // setting is bound by hand — and checked by hand, since ValidateOnStart has not run yet. A zero here
        // would not wait for startup validation to complain: it would be handed to the delivery service as is.
        var outbox = configuration
            .GetSection(RabbitMqSettings.SectionName)
            .Get<RabbitMqSettings>()?.Outbox ?? new OutboxSettings();

        if (outbox.QueryDelay <= TimeSpan.Zero)
            throw new InvalidOperationException("RabbitMq:Outbox:QueryDelay must be configured and positive.");

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
                
                // Pause between polls when there is nothing to deliver; with messages in the table, the
                // service takes the next batch (MessageDeliveryLimit) without sleeping. The default is
                // tens of seconds, which every test waiting for an event would pay. An idle cycle is not
                // free either — it locks a row in OutboxState, so replicas at this delay contend on one
                // hot row. Hence configuration rather than a literal: production keeps a calm interval
                // and the test factory shortens it, without either borrowing the other's value.
                o.QueryDelay = outbox.QueryDelay;
                
                // The consumer-side inbox is deliberately not enabled — idempotency lives in the unique
                // indexes — so this service spends every cycle cleaning a table that is always empty.
                o.DisableInboxCleanupService();
            });
            
            // Per-endpoint rather than bus-level: the consumer outbox will need its own filter on the same
            // endpoint later, and their order matters. Configuring both here keeps that decision in one place
            // instead of splitting it across cfg.UseMessageRetry and ConfigureEndpoints.
            x.AddConfigureEndpointsCallback((context, _, cfg) =>
            {
                var settings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
                
                cfg.UseMessageRetry(r => RetryPolicy.Configure(r, settings.Retry.Intervals));
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