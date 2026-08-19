using AccountingHelper.Application.DTOs.Responses;
using MediatR;

namespace AccountingHelper.Application.Features.Positions.Queries.GetPositions;

public record GetPositionsQuery : IRequest<IReadOnlyList<PositionResponse>>;
