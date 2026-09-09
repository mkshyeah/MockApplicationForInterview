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
        builder.UseSetting("Redis:ConnectionString", _redisConnectionString);
        
        var uri = new Uri(_rabbitMqConnectionString);
        var credentials = uri.UserInfo.Split(':', 2);

        builder.UseSetting("RabbitMq:Host", uri.Host);
        builder.UseSetting("RabbitMq:Port", uri.Port.ToString());
        builder.UseSetting("RabbitMq:VirtualHost", "/");                                                                                                                                                     
        builder.UseSetting("RabbitMq:UserName", Uri.UnescapeDataString(credentials[0]));                                                                                                                     
        builder.UseSetting("RabbitMq:Password", Uri.UnescapeDataString(credentials[1]));
        
    }
}
