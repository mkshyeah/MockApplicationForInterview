using AccountingHelper.Application.Features.Pagination;
using FluentValidation;

namespace AccountingHelper.Application.Features.Imports.Queries.GetImportErrors;

public class GetImportErrorsQueryValidator : AbstractValidator<GetImportErrorsQuery>
{
    public GetImportErrorsQueryValidator()
    {
        Include(new PagedQueryValidator<GetImportErrorsQuery>());
    }
}