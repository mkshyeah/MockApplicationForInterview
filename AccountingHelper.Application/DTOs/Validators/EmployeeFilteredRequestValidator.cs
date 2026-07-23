using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;

namespace AccountingHelper.Application.DTOs.Validators;

public class EmployeeFilteredRequestValidator : AbstractValidator<EmployeeFilteredRequest>
{
    public EmployeeFilteredRequestValidator()
    {
        RuleFor(e => e.OrderBy)
            .IsInEnum().WithMessage("OrderBy must be a valid value.");

        RuleFor(e => e.Direction)
            .IsInEnum().WithMessage("Direction must be a valid value.");

        RuleFor(e => e.EmployeeStatus)
            .IsInEnum().WithMessage("Employee status must be a valid value.");

        Include(new PaginationRequestValidator());
    }
}