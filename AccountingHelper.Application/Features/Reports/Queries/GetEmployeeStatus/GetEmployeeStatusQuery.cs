using AccountingHelper.Domain.Enums;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetEmployeeStatus;

public record GetEmployeeStatusQuery(Guid EmployeeId) : IRequest<EmployeeStatus>;