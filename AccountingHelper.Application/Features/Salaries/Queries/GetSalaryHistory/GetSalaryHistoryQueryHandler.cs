using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using AccountingHelper.Domain.Models;
using MediatR;

namespace AccountingHelper.Application.Features.Salaries.Queries.GetSalaryHistory;

public class GetSalaryHistoryQueryHandler : IRequestHandler<GetSalaryHistoryQuery, IReadOnlyList<Salary>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSalaryHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IReadOnlyList<Salary>> Handle(GetSalaryHistoryQuery request, CancellationToken ct)
    {
        var status = await _unitOfWork.Employees.GetStatusAsync(request.EmployeeId, ct);
        
        if (status == null)
            throw new NotFoundException("Employee", request.EmployeeId);
        
        var salaries = await _unitOfWork.Salaries
            .GetHistoryAsync(request.EmployeeId, ct);
        
        return salaries.ToList().AsReadOnly();
    }
}