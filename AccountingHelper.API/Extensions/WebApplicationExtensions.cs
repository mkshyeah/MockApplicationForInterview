using AccountingHelper.API.Middleware;
using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ApplyLocalMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        if (environment.IsEnvironment("local"))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }

        return app;
    }
}