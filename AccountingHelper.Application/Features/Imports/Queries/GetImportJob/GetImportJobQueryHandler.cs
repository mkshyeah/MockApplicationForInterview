using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Imports.Queries.GetImportJob;

public class GetImportJobQueryHandler : IRequestHandler<GetImportJobQuery, ImportJobResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetImportJobQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;  
    }
    public async Task<ImportJobResponse> Handle(GetImportJobQuery request, CancellationToken ct)
    {
        var job = await _unitOfWork.ImportJobs.GetByIdAsync(request.ImportJobId, ct);

        if (job == null)
        {
            throw new NotFoundException("ImportJob", request.ImportJobId);
        }

        return job.ToResponse();
    }
}