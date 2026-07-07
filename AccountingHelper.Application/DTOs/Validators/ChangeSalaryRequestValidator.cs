using AccountingHelper.Application.DTOs.Requests;
using FluentValidation;


namespace AccountingHelper.Application.DTOs.Validators;

public class ChangeSalaryRequestValidator : AbstractValidator<ChangeSalaryRequest>
{
    public ChangeSalaryRequestValidator()
    {
        RuleFor(salary => salary.Amount).NotEmpty().GreaterThan(0).LessThan(10_000_000);
        RuleFor(salary => salary.SalaryType).NotEmpty().IsInEnum();
    }
}