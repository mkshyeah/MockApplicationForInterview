using MediatR;

namespace AccountingHelper.Application.Features.Payroll.Commands.RunPayroll;

public record RunPayrollCommand(Guid PayrollRunId) : IRequest;