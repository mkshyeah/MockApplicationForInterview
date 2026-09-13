using FluentValidation;

namespace AccountingHelper.Application.Features.Payroll.Commands.CreatePayrollRun;

public class CreatePayrollRunCommandValidator : AbstractValidator<CreatePayrollRunCommand>
{
    public CreatePayrollRunCommandValidator()
    {
        RuleFor(x => x.Year)                                                                                                                                                                                     
            // Not a business rule, just a typo guard: catches 202 and 20255 before they                                                                                                                         
            // reach the database. The real bound is the period check below.                                                                                                                                     
            .InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");
        
        RuleFor(x => x.Month)                                                                                                                                                                                    
            .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");
        
        RuleFor(x => x)
            .Must(NotBeInTheFuture)
            .WithName("period")
            .WithMessage("Payroll cannot be run for a future period.");
    }

    private static bool NotBeInTheFuture(CreatePayrollRunCommand command)
    {
        // Guard against a Month that failed its own rule: without this, new DateOnly(y, 13, 1) throws                                                                                                           
        // and the client gets a 500 instead of the validation error that was already waiting for them.                                                                                                          
        if (command.Month is < 1 or > 12) return true;
        
        var now = DateTime.UtcNow;
        return command.Year < now.Year || (command.Year == now.Year && command.Month <= now.Month);
    }
}