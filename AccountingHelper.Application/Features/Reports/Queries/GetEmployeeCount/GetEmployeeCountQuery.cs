using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetEmployeeCount;

public record GetEmployeeCountQuery : IRequest<int>;