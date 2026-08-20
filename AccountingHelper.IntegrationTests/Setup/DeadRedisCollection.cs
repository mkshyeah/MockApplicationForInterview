using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

[CollectionDefinition("Integration:DeadRedis")]
public class DeadRedisCollection : ICollectionFixture<DeadRedisFixture>
{

}
