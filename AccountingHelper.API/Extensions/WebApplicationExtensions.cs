using AccountingHelper.API.Middleware;
using AccountingHelper.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AccountingHelper.API.Extensions;

public static class WebApplicationExtensions
{
    public static async Task ApplyLocalMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var environment = services.GetRequiredService<IWebHostEnvironment>();

        if (environment.IsEnvironment("local"))
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var retries = 5;
            while (retries > 0)
            {
                try
                {
                    logger.LogInformation("Applying local migrations...");
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully.");
                    break; 
                }
                catch (Exception ex)
                {
                    retries--;
                    logger.LogWarning(ex, "Could not apply migrations. Retrying in 3 seconds... ({RetriesLeft} retries left)", retries);
                
                    if (retries == 0)
                    {
                        logger.LogCritical(ex, "Database migration failed after maximum retries. Exiting application.");
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(3));
                }
            }
        }
    }
}