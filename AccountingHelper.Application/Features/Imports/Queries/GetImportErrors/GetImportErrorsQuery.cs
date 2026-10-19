using AccountingHelper.Application.DTOs.Responses;
using AccountingHelper.Application.Features.Pagination;
using MediatR;

namespace AccountingHelper.Application.Features.Imports.Queries.GetImportErrors;

public record GetImportErrorsQuery(Guid ImportJobId, int Offset, int Limit)
    : IRequest<GetImportErrorsResult>, IPagedQuery;

public record GetImportErrorsResult(IReadOnlyList<ImportErrorResponse> Items, int TotalCount);