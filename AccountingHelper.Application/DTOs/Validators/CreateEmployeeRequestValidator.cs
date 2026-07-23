using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;

namespace AccountingHelper.Application.DTOs.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(e => e.FirstName)
            .NotEmpty().WithMessage("First name cannot be empty.")
            .MaximumLength(100).WithMessage("First name must be at most 100 characters.");

        RuleFor(e => e.LastName)
            .NotEmpty().WithMessage("Last name cannot be empty.")
            .MaximumLength(100).WithMessage("Last name must be at most 100 characters.");

        RuleFor(e => e.Email)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(e => e.PositionId)
            .NotEmpty().WithMessage("Position is required.");

        RuleFor(e => e.Salary)
            .GreaterThan(0).WithMessage("Salary must be greater than 0.")
            .LessThan(10_000_000).WithMessage("Salary must be less than 10,000,000.");

        RuleFor(e => e.SalaryType)
            .IsInEnum().WithMessage("Salary type must be a valid value.");

        RuleFor(e => e.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");

        RuleFor(e => e.HireDate)
            .GreaterThan(new DateTime(1950, 1, 1))
            .WithMessage("Hire date must be no earlier than January 1, 1950.");
    }
}