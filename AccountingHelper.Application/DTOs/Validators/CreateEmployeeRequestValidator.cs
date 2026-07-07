using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;

namespace AccountingHelper.Application.DTOs.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(e => e.FirstName).MaximumLength(100).NotEmpty().WithMessage("First name cannot be empty");
        RuleFor(e => e.LastName).MaximumLength(100).NotEmpty().WithMessage("Last name cannot be empty");
        RuleFor(e => e.Email).EmailAddress().NotEmpty().WithMessage("Email cannot be empty");
        RuleFor(e => e.PositionId).NotEmpty().WithMessage("Position cannot be empty");
        RuleFor(e => e.Salary).NotEmpty().GreaterThan(0).LessThan(10_000_000);
        RuleFor(salary => salary.SalaryType).NotEmpty().IsInEnum();
        RuleFor(e => e.DepartmentId).NotEmpty().WithMessage("Department cannot be empty");
        RuleFor(e => e.HireDate).NotEmpty().WithMessage("Hire date cannot be empty.")
            .GreaterThan(new DateTime(1950, 1, 1))
            .WithMessage("The date of employment must be no earlier than January 1, 1950.");
    }
}