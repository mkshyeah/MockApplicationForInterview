using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Payroll.Queries.GetPayslips;

public class GetPayslipsQueryHandler : IRequestHandler<GetPayslipsQuery, IReadOnlyList<PayslipResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPayslipsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IReadOnlyList<PayslipResponse>> Handle(GetPayslipsQuery request, CancellationToken ct)
    {
        var run = await _unitOfWork.PayrollRuns
            .GetByIdAsync(request.PayrollRunId, ct);

        if (run == null)
        {
            throw new NotFoundException("PayrollRun", request.PayrollRunId);
        }

        var payslips = await _unitOfWork.Payslips.GetByRunAsync(run.Id, ct);
        return payslips.Select(p => p.ToResponse()).ToList();
    }
}