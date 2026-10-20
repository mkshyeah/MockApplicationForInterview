using System.Diagnostics;
using System.Text;
using AccountingHelper.Contracts.Messaging;
using AccountingHelper.IntegrationTests.Setup;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;
using Xunit.Sdk;

namespace AccountingHelper.IntegrationTests.Tests;

// RetryPolicyTests proves the shape of RetryPolicy on a bus it builds for itself. That is exactly why it missed
// a second, bus-level UseMessageRetry left in AddMessagingServices: the policy was right, the wiring around it
// was not. These tests go through the wiring the Worker really uses — WorkerTestHost calls AddMessagingServices
// with WorkerConsumers.Register — and judge it by what arrives in the error queue.
public class RetryWiringTests : IntegrationTestBase
{
    public RetryWiringTests(IntegrationFixture fixture) : base(fixture)
    {
    }

    private const string ErrorQueue = "import-run_error";

    // MassTransit declares the error queue lazily, on the first fault, and a basic.get on a queue that does not
    // exist closes the channel — so each poll gets a fresh channel, and "no queue yet" counts as "empty".
    private async Task<BasicGetResult> WaitForErrorAsync(TimeSpan limit)
    {
        var factory = new ConnectionFactory { Uri = new Uri(RabbitMqConnectionString) };
        await using var connection = await factory.CreateConnectionAsync();

        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < limit)
        {
            try
            {
                await using var channel = await connection.CreateChannelAsync();

                var message = await channel.BasicGetAsync(ErrorQueue, autoAck: true);
                if (message is not null) return message;
            }
            catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
            {
            }

            await Task.Delay(50);
        }

        throw new XunitException($"Nothing reached {ErrorQueue} within {limit}.");
    }

    private static string Header(BasicGetResult message, string name) =>
        message.BasicProperties.Headers is { } headers && headers.TryGetValue(name, out var value)
            ? value is byte[] bytes ? Encoding.UTF8.GetString(bytes) : value?.ToString() ?? ""
            : "";

    [Fact]
    public async Task PermanentFailure_ReachesTheErrorQueueWithoutRetries()
    {
        // ARRANGE
        await using var worker = await WorkerTestHost.StartAsync(
            Services.GetRequiredService<IConfiguration>());

        // ACT
        // A job that does not exist: RunImportCommandHandler throws NotFoundException, an IPermanentFailure.
        // Published straight on the API host's bus — the outbox is not what is under test here.
        var stopwatch = Stopwatch.StartNew();
        await Services.GetRequiredService<IBus>().Publish(new ImportRequested(Guid.NewGuid()));

        var faulted = await WaitForErrorAsync(TimeSpan.FromSeconds(30));
        stopwatch.Stop();

        // ASSERT
        // MT-Fault-RetryCount is MassTransit's own account of how many times it tried again; the elapsed time is the
        // symptom a user would see. With the leftover bus-level policy it was 3 retries and about 15 s: that
        // policy had no Ignore for IPermanentFailure and wrapped the endpoint's one.
        Header(faulted, "MT-Fault-ExceptionType").Should().EndWith("NotFoundException");
        // MassTransit writes the header only once something was retried, so "no retries" is its absence.
        Header(faulted, "MT-Fault-RetryCount").Should().BeEmpty();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
