using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;

namespace AccountingHelper.Application.DTOs.Validators;

public class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    public PaginationRequestValidator()
    {
        RuleFor(p => p.Offset)
            .GreaterThanOrEqualTo(0).WithMessage("Offset must be greater than or equal to 0.");

        RuleFor(p => p.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit must be between 1 and 100.");
    }
}