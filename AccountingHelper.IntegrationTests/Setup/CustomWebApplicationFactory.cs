using AccountingHelper.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccountingHelper.IntegrationTests.Setup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _redisConnectionString;

    public CustomWebApplicationFactory(string connectionString, string redisConnectionString)
    {
        _connectionString = connectionString;
        _redisConnectionString = redisConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);

        // Адрес Redis приходит снаружи: IntegrationFixture передаёт сюда контейнер,
        // DeadRedisFixture — заведомо недоступный endpoint. Строка задаётся целиком,
        // потому что UseSetting перекрывает значение, а не сливает параметры.
        builder.UseSetting("Redis:ConnectionString", _redisConnectionString);

        // Redis:InstanceName сознательно НЕ переопределяем: он приходит из базового
        // appsettings.json, и тесты обязаны работать с той же схемой ключей, что прод.
        // Это зафиксировано тестом RedisInstanceName_ComesFromBaseAppSettings.
    }
}
