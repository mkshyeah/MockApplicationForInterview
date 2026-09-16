using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Payroll.Queries.GetPayslips;

public record GetPayslipsQuery(Guid PayrollRunId) : IRequest<IReadOnlyList<PayslipResponse>>;