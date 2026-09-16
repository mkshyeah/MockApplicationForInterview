using System.Net;
using System.Net.Http.Json;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.Domain.Enums;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class OutboxTests : IntegrationTestBase
{
    public OutboxTests(IntegrationFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SalaryChange_WhenBrokerIsDownAtCommitTime_SucceedsAndDeliversAfterRecovery()
    {
        //ARRANGE
        // The spy starts before the outage so its queue and binding already exist. Declare them
        // after the fact and the recovered message lands in _skipped instead.
        await using var spy = await MessageSpyHost.StartAsync(RabbitMqConnectionString);

        var departmentId = await SeedDepartmentAsync("IT");
        var positionId = await SeedPositionAsync("Junior Developer", EmployeeGrade.Junior);
        var employee = await CreateEmployeeAsync(departmentId, positionId, salary: 1000m);
        
        HttpStatusCode? status;

        await PauseBrokerAsync();
        try
        {
            //ACT
            status = await TryChangeSalaryAsync(employee.Id, 10_000m);
        }
        finally
        {
            // Must run even if the act above throws: a paused broker would take down every
            // remaining test in the run, and the failure would point anywhere but here.
            await ResumeBrokerAsync();
        }
        
        //ASSERT
        // Writing the data and telling the caller it failed is the dual-write defect in one line.
        status.Should().Be(HttpStatusCode.OK, "committing the salary must not depend on the broker");

        await WithDbContextAsync(async db =>
        {
            var salary = await db.Salaries.SingleAsync(s => s.EmployeeId == employee.Id && s.EndDate == null);
            salary.Amount.Should().Be(10_000m);
        });
        // The event survived the outage because it was committed with the data, not sent alongside it.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        (await spy.Harness.Consumed.Any<SalaryChanged>(cts.Token)).Should().BeTrue();
    }

    private async Task<HttpStatusCode?> TryChangeSalaryAsync(Guid employeeId, decimal amount)
    {
        // Publish on a disconnected bus waits for the connection rather than failing fast, so the
        // request can hang instead of erroring. The deadline rides on the request rather than on
        // Client.Timeout, which cannot be changed once the client has sent anything.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        try
        {
            var resp = await Client.PutAsJsonAsync(
                $"v1/employees/{employeeId}/salaries",
                new { amount, salaryType = "Monthly" },
                cts.Token);

            return resp.StatusCode;
        }
        catch (OperationCanceledException)
        {
            // No answer at all is one of the shapes this defect takes.
            return null;
        }
    }
}