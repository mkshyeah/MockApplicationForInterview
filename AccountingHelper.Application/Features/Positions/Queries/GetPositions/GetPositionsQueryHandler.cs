using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Positions.Queries.GetPositions;

public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, IReadOnlyList<PositionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPositionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PositionResponse>> Handle(GetPositionsQuery request, CancellationToken ct)
    {
        var items = await _unitOfWork.Positions.GetAllAsync(ct);

        return items.Select(x => x.ToResponse()).ToList();
    }
}
