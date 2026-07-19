using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Domain.Enums;
using AccountingHelper.Infrastructure.Contexts;
using AccountingHelper.Infrastructure.Data.Entities;
using FluentAssertions;
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
    
    protected async Task<EmployeeResponse> CreateEmployeeAsync(
        Guid departmentId,
        Guid positionId,
        decimal salary = 1000m,
        string salaryType = "Monthly",
        string hireDate = "2026-01-01T00:00:00Z",
        string firstName = "Emp",
        string lastName = "Test",
        string? email = null)
    {
        email ??= $"{Guid.NewGuid():N}@test.com";
 
        var resp = await Client.PostAsJsonAsync("v1/employees", new
        {
            firstName, lastName, email,
            departmentId, positionId,
            salary, salaryType, hireDate
        });
 
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
 
        var body = await resp.Content.ReadFromJsonAsync<EmployeeResponse>(Json);
        return body!;
    }
}