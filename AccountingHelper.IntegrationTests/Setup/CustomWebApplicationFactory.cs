using AccountingHelper.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccountingHelper.IntegrationTests.Setup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _redisConnectionString;
    private readonly string _rabbitMqConnectionString;

    public CustomWebApplicationFactory(
        string connectionString,
        string redisConnectionString,
        string rabbitMqConnectionString)
    {
        _connectionString = connectionString;
        _redisConnectionString = redisConnectionString;
        _rabbitMqConnectionString = rabbitMqConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);

        // Endpoints come from outside: the live fixtures pass a container, the dead ones an
        // address nothing answers on. Each connection string is set whole, because UseSetting
        // overwrites the value rather than merging parameters into it.
        builder.UseSetting("Redis:ConnectionString", _redisConnectionString);
        builder.UseSetting("Cache:Dashboard", "01:00:00");

        // Redis:InstanceName is deliberately NOT overridden: it comes from the base
        // appsettings.json, and tests have to run against the same key scheme as production.
        // RedisInstanceName_ComesFromBaseAppSettings guards this.

        // RabbitMqSettings keeps host, port and credentials apart while Testcontainers hands
        // out an amqp:// URI, so the split happens here — the production settings class does
        // not grow a second way to be configured just for tests.
        var uri = new Uri(_rabbitMqConnectionString);
        var credentials = uri.UserInfo.Split(':', 2);

        builder.UseSetting("RabbitMq:Host", uri.Host);
        builder.UseSetting("RabbitMq:Port", uri.Port.ToString());
        builder.UseSetting("RabbitMq:VirtualHost", "/");
        builder.UseSetting("RabbitMq:UserName", Uri.UnescapeDataString(credentials[0]));
        builder.UseSetting("RabbitMq:Password", Uri.UnescapeDataString(credentials[1]));
        builder.UseSetting("RabbitMq:Outbox:QueryDelay", "00:00:01");
        // Same policy shape as production — five attempts, two of them immediate — at a speed a test
        // suite can afford. Overriding the shape instead of the timings would leave the real one untested.
        builder.UseSetting("RabbitMq:Retry:Intervals:0", "00:00:00");
        builder.UseSetting("RabbitMq:Retry:Intervals:1", "00:00:00");
        builder.UseSetting("RabbitMq:Retry:Intervals:2", "00:00:00.010");
        builder.UseSetting("RabbitMq:Retry:Intervals:3", "00:00:00.010");
        builder.UseSetting("RabbitMq:Retry:Intervals:4", "00:00:00.010");
    }
}
