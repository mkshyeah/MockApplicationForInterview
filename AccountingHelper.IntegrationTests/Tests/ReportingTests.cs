using AccountingHelper.IntegrationTests.Setup;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ReportingTests : IntegrationTestBase
{
    public ReportingTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetEmployeeCount_ReturnsTotalNumberOfEmployees()
    {
        
    }

    [Fact]
    public async Task GetActiveSalarySum_ReturnsSumOfActiveSalaries()
    {
        
    }

    [Fact]
    public async Task GetEmployeeTaxes_ReturnsMonthlyTaxForActiveSalary()
    {
        
    }

    [Fact]
    public async Task GetEmployeeStatus_WhenNotFound_Returns404()
    {
        
    }
}