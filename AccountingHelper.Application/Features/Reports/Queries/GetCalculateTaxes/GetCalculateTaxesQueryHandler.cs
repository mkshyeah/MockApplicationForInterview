using AccountingHelper.Application.Exceptions;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Reports.Queries.GetCalculateTaxes;

public class GetCalculateTaxesQueryHandler : IRequestHandler<GetCalculateTaxesQuery, decimal>
{
    private IUnitOfWork _unitOfWork;
    public GetCalculateTaxesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;   
    }
    
    public async Task<decimal> Handle(GetCalculateTaxesQuery request, CancellationToken ct)
    {
        var salary = await _unitOfWork.Salaries
            .GetCurrentSalaryAsync(request.EmployeeId, ct);
        
        if (salary == null)
            throw new NotFoundException("Employee Salary", request.EmployeeId);

        return salary.CalculateTaxes();
    }
}