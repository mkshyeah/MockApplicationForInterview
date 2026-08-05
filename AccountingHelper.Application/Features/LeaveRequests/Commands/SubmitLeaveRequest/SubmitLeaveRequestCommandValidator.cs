using FluentValidation;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    private static DateOnly EarliestStart => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
    
    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee id cannot be empty.");
            
        RuleFor(x => x.LeaveType)
            .IsInEnum().WithMessage("LeaveType must be a valid value");
        
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(_ => EarliestStart)
            .WithMessage("Start date must not be earlier than one month ago.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after start date.");
    }
}