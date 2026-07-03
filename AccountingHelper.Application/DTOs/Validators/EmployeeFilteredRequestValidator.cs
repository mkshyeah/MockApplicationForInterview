using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;

namespace AccountingHelper.Application.DTOs.Validators;

public class EmployeeFilteredRequestValidator : AbstractValidator<EmployeeFilteredRequest>
{
    public EmployeeFilteredRequestValidator()
    {
        RuleFor(e => e.OrderBy).NotEmpty().IsInEnum();
        RuleFor(e => e.Direction).NotEmpty().IsInEnum();
        RuleFor(e => e.EmployeeStatus).IsInEnum();
        Include(new PaginationRequestValidator());
    }
}