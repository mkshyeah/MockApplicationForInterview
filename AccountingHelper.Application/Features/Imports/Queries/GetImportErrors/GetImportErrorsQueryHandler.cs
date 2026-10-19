using AccountingHelper.Application.Exceptions;
using AccountingHelper.Application.Mapping;
using AccountingHelper.Domain.Interfaces;
using MediatR;

namespace AccountingHelper.Application.Features.Imports.Queries.GetImportErrors;

public class GetImportErrorsQueryHandler : IRequestHandler<GetImportErrorsQuery, GetImportErrorsResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetImportErrorsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<GetImportErrorsResult> Handle(GetImportErrorsQuery request, CancellationToken ct)
    {
        // An unknown job is a 404, not an empty page: "no errors" is a real answer about a real job, and the
        // client must be able to tell it apart from a typo in the id.
        var job = await _unitOfWork.ImportJobs.GetByIdAsync(request.ImportJobId, ct);

        if (job == null)
        {
            throw new NotFoundException("ImportJob", request.ImportJobId);
        }

        var (errors, total) = await _unitOfWork.ImportJobs.GetErrorsAsync(job.Id, request.Offset, request.Limit, ct);

        return new GetImportErrorsResult(
            Items: errors.Select(e => e.ToResponse()).ToList(),
            TotalCount: total);
    }
}