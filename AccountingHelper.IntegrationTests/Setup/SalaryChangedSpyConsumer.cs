using AccountingHelper.Contracts.Messaging;
using MassTransit;

namespace AccountingHelper.IntegrationTests.Setup;

public class SalaryChangedSpyConsumer : IConsumer<SalaryChanged>
{
    public Task Consume(ConsumeContext<SalaryChanged> context) => Task.CompletedTask;
}