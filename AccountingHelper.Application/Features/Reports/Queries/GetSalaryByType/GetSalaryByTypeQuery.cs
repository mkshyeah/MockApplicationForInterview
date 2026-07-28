using AccountingHelper.Domain.Enums;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetSalaryByType;

public record GetSalaryByTypeQuery(Guid EmployeeId, SalaryType Type) : IRequest<decimal>;