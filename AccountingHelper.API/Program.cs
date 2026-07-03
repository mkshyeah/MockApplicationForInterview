using AccountingHelper.API.Extensions;
using AccountingHelper.Application.Extensions;
using AccountingHelper.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация и слои
builder.Configuration.AddCustomConfiguration(builder.Environment);

builder.Services
    .AddApiServices(builder.Configuration) // Локализация, Swagger и Контроллеры теперь внутри!
    .AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices();

var app = builder.Build();

app.UseExceptionHandler();

// Наш чистый метод автоматической миграции
app.ApplyLocalMigrations(); 

if (app.Environment.IsEnvironment("local"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();