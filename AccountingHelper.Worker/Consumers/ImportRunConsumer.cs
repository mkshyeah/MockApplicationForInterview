using AccountingHelper.Application.Features.Imports.Commands.RunImport;
using AccountingHelper.Contracts.Messaging;
using MassTransit;
using MediatR;

namespace AccountingHelper.Worker.Consumers;

public sealed class ImportRunConsumer : IConsumer<ImportRequested>
{
    private readonly ISender _sender;

    public ImportRunConsumer(ISender sender) => _sender = sender;

    public Task Consume(ConsumeContext<ImportRequested> context)
    {
        return _sender.Send(new RunImportCommand(context.Message.ImportJobId), context.CancellationToken);
    }
}