using AccountingHelper.Infrastructure.Extensions;
using AccountingHelper.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingHelper.UnitTests.Infrastructure;

// Reads the committed base appsettings.json of each host and binds it the way the hosts do. Two failures hid
// here, and neither showed up anywhere else:
// - the Worker had no RabbitMq section, so it ran on code defaults (a 1 s outbox poll) without a word;
// - ConfigurationBinder appends array elements from configuration to an array that already has items, so a
//   default on Retry.Intervals turned the API's five configured intervals into ten.
public class MessagingSettingsTests
{
    private static readonly string[] Hosts = ["AccountingHelper.API", "AccountingHelper.Worker"];

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AccountingHelper.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("AccountingHelper.sln not found above the test output.");
    }

    private static IConfigurationSection BaseRabbitMqSection(string host) =>
        new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(RepositoryRoot(), host, "appsettings.json"))
            .Build()
            .GetSection(RabbitMqSettings.SectionName);

    [Theory]
    [MemberData(nameof(HostNames))]
    public void BaseAppSettings_BindRetryIntervals_ExactlyAsWritten(string host)
    {
        // ARRANGE
        var section = BaseRabbitMqSection(host);
        var written = section.GetSection("Retry:Intervals").GetChildren().Count();

        // ACT
        var settings = section.Get<RabbitMqSettings>()!;

        // ASSERT
        // Equal to the number of entries in the file, not double it: nothing may be prepended by a default.
        written.Should().BePositive("{0} must declare its retry policy rather than inherit one from code", host);
        settings.Retry.Intervals.Should().HaveCount(written);
    }

    [Theory]
    [MemberData(nameof(HostNames))]
    public void BaseAppSettings_DeclareOutboxQueryDelay(string host)
    {
        // ACT
        var settings = BaseRabbitMqSection(host).Get<RabbitMqSettings>();

        // ASSERT
        settings.Should().NotBeNull("{0} must have a RabbitMq section in its base appsettings.json", host);
        settings!.Outbox.QueryDelay.Should().BePositive();
    }

    [Fact]
    public void BaseAppSettings_GiveBothHostsTheSameMessagingPolicy()
    {
        // ACT
        var api = BaseRabbitMqSection(Hosts[0]).Get<RabbitMqSettings>()!;
        var worker = BaseRabbitMqSection(Hosts[1]).Get<RabbitMqSettings>()!;

        // ASSERT
        // Repeated in both files like Cache and Import, because composition roots may repeat themselves —
        // which is exactly why a test has to notice when the copies drift.
        worker.Retry.Intervals.Should().Equal(api.Retry.Intervals);
        worker.Outbox.QueryDelay.Should().Be(api.Outbox.QueryDelay);
    }

    [Fact]
    public void AddMessagingServices_WithoutOutboxQueryDelay_FailsAtRegistration()
    {
        // ARRANGE
        var configuration = new ConfigurationBuilder().Build();

        // ACT
        var act = () => new ServiceCollection().AddMessagingServices(configuration);

        // ASSERT
        // Read before ValidateOnStart can run, so it has to fail here — a zero would otherwise reach the
        // delivery service as an instruction to poll without pausing.
        act.Should().Throw<InvalidOperationException>().WithMessage("*RabbitMq:Outbox:QueryDelay*");
    }

    public static TheoryData<string> HostNames() => new(Hosts);
}
