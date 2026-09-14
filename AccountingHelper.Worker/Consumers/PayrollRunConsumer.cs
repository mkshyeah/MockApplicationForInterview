using AccountingHelper.Application.Features.Payroll.Commands.RunPayroll;
using AccountingHelper.Contracts.Messaging;
using MassTransit;
using MediatR;

namespace AccountingHelper.Worker.Consumers;

public sealed class PayrollRunConsumer : IConsumer<PayrollRunRequested>
{
    private readonly ISender _sender;

    public PayrollRunConsumer(ISender sender) => _sender = sender;
    
    public Task Consume(ConsumeContext<PayrollRunRequested> context)
    {
        return _sender.Send(new RunPayrollCommand(context.Message.PayrollRunId), context.CancellationToken);
    }
}