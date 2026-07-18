using AccountingHelper.IntegrationTests.Setup;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

public class EmployeeCreateTests : IntegrationTestBase
{
    public EmployeeCreateTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateEmployee_WithValidData_Returns201AndPersists()
    {
        throw  new NotImplementedException();
    }

    [Fact]
    public async Task CreateEmployee_CreatesInitialActiveSalary()
    {
        throw  new NotImplementedException();
    }

    [Fact]
    public async Task CreateEmployee_ThenGetById_ReturnsSameEmployee()
    {
        throw  new NotImplementedException();
    }

    [Fact]
    public async Task reateEmployee_WithDuplicateEmail_Returns409()
    {
        throw  new NotImplementedException();
    }

    [Fact]
    public async Task CreateEmployee_WithNonexistentDepartment_Returns404()
    {
        throw  new NotImplementedException();
    }
}