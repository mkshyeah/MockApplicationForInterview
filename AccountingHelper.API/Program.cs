using AccountingHelper.API.Extensions;
using AccountingHelper.API.Middleware;
using AccountingHelper.Application.Extensions;
using AccountingHelper.Infrastructure.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Serilog.Templates.Themes;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(new ExpressionTemplate(
        "[{@t:HH:mm:ss} {@l:u3}] {@m}\n{@x}", 
        theme: TemplateTheme.Code))
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application...");
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));
    
    builder.Configuration.AddCustomConfiguration(builder.Environment);
    

    builder.Services
        .AddApiServices(builder.Configuration) 
        .AddInfrastructureServices(builder.Configuration)
        .AddApplicationServices();

    await using var app = builder.Build();

    app.UseExceptionHandler();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<CorrelationIdLoggingMiddleware>();
    app.UseSerilogRequestLogging();
    
    app.ApplyLocalMigrations();

    if (app.Environment.IsEnvironment("local"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.MapControllers();
    
    await app.RunAsync();
    
    Log.Information("Stopped cleanly");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}










