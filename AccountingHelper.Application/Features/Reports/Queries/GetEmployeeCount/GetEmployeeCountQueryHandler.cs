using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetEmployeeCount;

public class GetEmployeeCountQueryHandler : IRequestHandler<GetEmployeeCountQuery, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEmployeeCountQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(GetEmployeeCountQuery request, CancellationToken ct)
    {
        return await _unitOfWork.Employees.CountAsync(ct);
    }
}