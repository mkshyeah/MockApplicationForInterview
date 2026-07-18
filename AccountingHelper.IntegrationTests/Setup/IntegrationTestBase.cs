using System.Text.Json;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

[Collection("Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    protected HttpClient Client { get; private set; } = null!;
    protected JsonSerializerOptions Json { get; private set; } = null!;

    public IntegrationTestBase(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }
    
    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        Client = _fixture.CreateClient();
        Json = _fixture.Factory.Services
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value.JsonSerializerOptions;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
    
    protected async Task<T> WithDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(db);
    }
    
    protected Task WithDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        return WithDbContextAsync(async db =>
        {
            await action(db);
            return true;
        });
    }
    
    protected Task<(Guid DepartmentId, Guid PositionId)> SeedReferenceDataAsync()
    {
        return WithDbContextAsync(async db =>
        {
            var department = new DepartmentEntity
            {
                Id = Guid.NewGuid(),
                Name = "Test Department"
            };
 
            var position = new PositionEntity
            {
                Id = Guid.NewGuid(),
                Title = "Test Position",
                Grade = EmployeeGrade.Junior
            };
 
            db.Departments.Add(department);
            db.Positions.Add(position);
            await db.SaveChangesAsync();
 
            return (department.Id, position.Id);
        });
    }
}