using AccountingHelper.Domain.Enums;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Services;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

// Against a real database on purpose: a mock proves neither the ANY(...) translation nor what the
// change tracker actually does with rows staged before a clear.
public class ImportPersistenceTests : IntegrationTestBase
{
    public ImportPersistenceTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetExistingEmailsAsync_ReturnsOnlyEmailsThatExist()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        await CreateEmployeeAsync(departmentId, positionId, email: "a@test.com");
        await CreateEmployeeAsync(departmentId, positionId, email: "b@test.com");

        using var scope = Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // ACT
        var existing = await unitOfWork.Employees.GetExistingEmailsAsync(
            ["a@test.com", "b@test.com", "new@test.com"], CancellationToken.None);

        // ASSERT
        existing.Should().BeEquivalentTo(["a@test.com", "b@test.com"]);
    }

    [Fact]
    public async Task GetExistingEmailsAsync_ComparesAsGiven_WithoutNormalizing()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();
        await CreateEmployeeAsync(departmentId, positionId, email: "a@test.com");

        using var scope = Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // ACT
        var existing = await unitOfWork.Employees.GetExistingEmailsAsync(["A@TEST.COM"], CancellationToken.None);

        // ASSERT
        // Normalizing is the caller's job. A repository that quietly did it too would hide a caller that forgot,
        // and the unique index would still reject that caller's insert.
        existing.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExistingEmailsAsync_WithEmptyInput_ReturnsEmpty()
    {
        // ARRANGE
        using var scope = Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // ACT
        var existing = await unitOfWork.Employees.GetExistingEmailsAsync([], CancellationToken.None);

        // ASSERT
        existing.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearTracking_DropsPendingInserts()
    {
        // ARRANGE
        var (departmentId, positionId) = await SeedReferenceDataAsync();

        using var scope = Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var (employee, balances) = EmployeeFactory.Hire(
            "John", "Doe", "cleared@test.com",
            departmentId, positionId, DateTime.UtcNow.Date,
            1000m, SalaryType.Monthly);

        unitOfWork.Employees.Add(employee);
        foreach (var balance in balances)
            unitOfWork.LeaveBalances.Add(balance);

        // ACT
        // Exactly what a failed batch leaves behind: rows staged as Added in a context that is about to save again.
        unitOfWork.ClearTracking();
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        // ASSERT
        await WithDbContextAsync(async db =>
        {
            (await db.Employees.AnyAsync(e => e.Email == "cleared@test.com")).Should().BeFalse();
            (await db.LeaveBalances.AnyAsync(b => b.EmployeeId == employee.Id)).Should().BeFalse();
        });
    }
}
