using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetTotalSalaries;

public record GetTotalSalariesQuery : IRequest<decimal>;