using Xunit;

namespace AccountingHelper.IntegrationTests.Setup;

[CollectionDefinition("Integration:DeadBroker")]
public class DeadBrokerCollection : ICollectionFixture<DeadBrokerFixture> { }