using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Positions.Queries.GetPositions;

public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, IReadOnlyList<Position>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPositionsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Position>> Handle(GetPositionsQuery request, CancellationToken ct)
    {
        return await _unitOfWork.Positions.GetAllAsync(ct);
    }
}
