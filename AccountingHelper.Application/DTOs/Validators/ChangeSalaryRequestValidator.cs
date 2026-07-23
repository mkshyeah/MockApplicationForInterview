using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;


namespace AccountingHelper.Application.DTOs.Validators;

public class ChangeSalaryRequestValidator : AbstractValidator<ChangeSalaryRequest>
{
    public ChangeSalaryRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .LessThan(10_000_000).WithMessage("Amount must be less than 10,000,000.");
        
        RuleFor(x => x.SalaryType)
            .IsInEnum().WithMessage("SalaryType must be a valid value.");
    }
}