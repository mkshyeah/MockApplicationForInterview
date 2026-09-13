using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Payroll.Commands.CreatePayrollRun;

public record CreatePayrollRunCommand(
    int Year,
    int Month) : IRequest<PayrollRunResponse>;