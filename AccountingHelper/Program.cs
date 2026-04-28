using AccountingHelper.Contexts;
using AccountingHelper.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
       .AddJsonFile("appsettings.json", true, true);
if (builder.Environment.IsEnvironment("local"))
{
    builder.Configuration.AddJsonFile("appsettings.local.json", true, true);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
                                                        options.UseSqlServer(
                                                            builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddScoped<IReportControllerService, ReportControllerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsEnvironment("local"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();