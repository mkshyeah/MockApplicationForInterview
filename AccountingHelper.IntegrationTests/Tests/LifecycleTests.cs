using AccountingHelper.IntegrationTests.Setup;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class LifecycleTests : IntegrationTestBase
{
    public LifecycleTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task FireEmployee_WhenActive_SetsFiredStatusAndClosesSalary()
    {
        
    }

    [Fact]
    public async Task FireEmployee_WhenAlreadyFired_Returns422()
    {
        
    }

    [Fact]
    public async Task SendOnVacation_WhenActive_SetsOnVacationStatus()
    {
        
    }

    [Fact]
    public async Task SendOnVacation_WhenFired_Returns422()
    {
        
    }

    [Fact]
    public async Task SendOffVacation_WhenNotOnVacation_Returns422()
    {
        
    }
}