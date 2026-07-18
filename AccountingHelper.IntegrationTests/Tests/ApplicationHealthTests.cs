using System.Net;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class ApplicationHealthTests : IntegrationTestBase
{
    public ApplicationHealthTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHealthLive_WhenDatabaseReachable_Returns200()
    {
        //ARRANGE
        
        //ACT
        var resp = await Client.GetAsync("/health/live");
        
        //ASSERT
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        
    }
}