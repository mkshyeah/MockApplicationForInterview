using FluentValidation;

namespace AccountingHelper.Application.Features.Salaries.Commands.ChangeSalary;

public class ChangeSalaryCommandValidator : AbstractValidator<ChangeSalaryCommand>
{
    public ChangeSalaryCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .LessThan(10_000_000).WithMessage("Amount must be less than 10,000,000.");
        
        RuleFor(x => x.SalaryType)
            .IsInEnum().WithMessage("SalaryType must be a valid value.");
    }
}