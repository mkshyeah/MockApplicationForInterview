using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetTotalSalaries;

public class GetTotalSalariesQueryHandler : IRequestHandler<GetTotalSalariesQuery, decimal>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTotalSalariesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<decimal> Handle(GetTotalSalariesQuery request, CancellationToken ct)
    {
        return await _unitOfWork.Salaries.GetTotalCurrentSalaryAsync(ct);
    }
}