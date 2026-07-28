using FluentValidation;

namespace AccountingHelper.Application.Features.Pagination;

public class PagedQueryValidator<T> : AbstractValidator<T>  where T : IPagedQuery
{
    public PagedQueryValidator()
    {
        RuleFor(p => p.Offset)
            .GreaterThanOrEqualTo(0).WithMessage("Offset must be greater than or equal to 0.");

        RuleFor(p => p.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit must be between 1 and 100.");
    }
}