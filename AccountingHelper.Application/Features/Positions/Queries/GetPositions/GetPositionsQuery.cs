using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Positions.Queries.GetPositions;

public record GetPositionsQuery : IRequest<IReadOnlyList<Position>>;
