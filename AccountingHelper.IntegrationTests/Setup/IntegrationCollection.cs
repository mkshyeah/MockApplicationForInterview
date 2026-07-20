using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    
}