using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Payroll.Queries.GetPayrollRun;

public record GetPayrollRunQuery(Guid PayrollRunId) : IRequest<PayrollRunResponse>;
