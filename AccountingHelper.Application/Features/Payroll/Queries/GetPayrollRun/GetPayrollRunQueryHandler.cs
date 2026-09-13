using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Payroll.Queries.GetPayrollRun;

public class GetPayrollRunQueryHandler : IRequestHandler<GetPayrollRunQuery, PayrollRunResponse>
{
    private IUnitOfWork _unitOfWork;

    public GetPayrollRunQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<PayrollRunResponse> Handle(GetPayrollRunQuery request, CancellationToken ct)
    {
        var payrollRun = await _unitOfWork.PayrollRuns.GetByIdAsync(request.PayrollRunId, ct);
        
        if(payrollRun == null)
            throw new NotFoundException("PayrollRun", request.PayrollRunId);

        return payrollRun.ToResponse();
    }
}