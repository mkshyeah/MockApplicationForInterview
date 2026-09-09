using AccountingHelper.Contracts;
using AccountingHelper.IntegrationTests.Setup;
using AccountingHelper.Worker.Consumers;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AccountingHelper.IntegrationTests.Tests;

[Collection("Integration")]
public class MessagingRoundTripTests
{
    private readonly IntegrationFixture _fixture;

    public MessagingRoundTripTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishedMessage_IsConsumed()
    {
        // ARRANGE
        await using var provider = new ServiceCollection()                                                                                                                                                   
              .AddMassTransitTestHarness(x =>                                                                                                                                                                  
              {                                                                                                                                                                                                
                  x.SetKebabCaseEndpointNameFormatter();                                                                                                                                                       
                  x.AddConsumer<PingConsumer>();                                                                                                                                                               
                  x.UsingRabbitMq((context, cfg) =>                                                                                                                                                            
                  {                                                                                                                                                                                            
                      cfg.Host(new Uri(_fixture.RabbitMqConnectionString));                                                                                                                                    
                      cfg.ConfigureEndpoints(context);                                                                                                                                                         
                  });                                                                                                                                                                                          
              })                                                                                                                                                                                               
              .AddLogging()                                                                                                                                                                                    
              .BuildServiceProvider(true);     
        
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        
        // ACT
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var pingId = Guid.NewGuid();
        
        await publisher.Publish(new PingRequested(pingId));
        
        // ASSERT
        (await harness.Consumed.Any<PingRequested>(x => x.Context.Message.PingId == pingId))
            .Should().BeTrue();
    }
}