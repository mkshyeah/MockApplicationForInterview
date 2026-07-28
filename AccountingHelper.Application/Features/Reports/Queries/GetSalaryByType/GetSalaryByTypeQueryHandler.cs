using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetSalaryByType;

public class GetSalaryByTypeQueryHandler : IRequestHandler<GetSalaryByTypeQuery, decimal>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSalaryByTypeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<decimal> Handle(GetSalaryByTypeQuery request, CancellationToken ct)
    {
        var salary = await _unitOfWork.Salaries.GetCurrentSalaryAsync(request.EmployeeId,ct);
        
        if(salary == null)
            throw new NotFoundException("Employee Salary", request.EmployeeId);

        return salary.ConvertTo(request.Type);
    }
}