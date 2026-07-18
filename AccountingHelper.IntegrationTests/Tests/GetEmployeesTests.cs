using AccountingHelper.IntegrationTests.Setup;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

[Collection("Integration")]
public class GetEmployeesTests : IntegrationTestBase
{
    public GetEmployeesTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    public async Task GetEmployees_WithOffsetAndLimit_ReturnsPagedSubset()
    {
        //ARRANGE
        
    }

    public async Task GetEmployees_ReturnsCorrectTotalInPagedResponse()
    {
        
    }

    public async Task GetEmployee_WhenNotFound_Returns404()
    {
        
    }
        
}