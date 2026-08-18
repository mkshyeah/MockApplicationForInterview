using AccountingHelper.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AccountingHelper.IntegrationTests.Setup;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public CustomWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        // deliberately unreachable: the test environment has no Redis, which pins down the
        // fail-open policy (cache is an optimisation, not a source of truth).
        // Block 3 replaces this with a Testcontainers.Redis endpoint.
        builder.UseSetting(
            "Redis:ConnectionString",
            "localhost:6399,abortConnect=false,connectTimeout=500,syncTimeout=500,connectRetry=1");
    }
}