using AccountingHelper.Application.Features.Pagination;
using FluentValidation;

namespace AccountingHelper.Application.Features.Employees.Queries.GetEmployees;

public class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(e => e.OrderBy)
            .IsInEnum().WithMessage("OrderBy must be a valid value.");

        RuleFor(e => e.Direction)
            .IsInEnum().WithMessage("Direction must be a valid value.");

        RuleFor(e => e.EmployeeStatus)
            .IsInEnum().WithMessage("Employee status must be a valid value.");
        
        Include(new PagedQueryValidator<GetEmployeesQuery>());
    }
}