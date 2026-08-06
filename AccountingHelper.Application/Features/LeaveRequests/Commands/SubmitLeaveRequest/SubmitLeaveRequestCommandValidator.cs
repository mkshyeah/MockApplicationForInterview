using FluentValidation;

namespace AccountingHelper.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

public class SubmitLeaveRequestCommandValidator : AbstractValidator<SubmitLeaveRequestCommand>
{
    /// <summary>
    /// Deliberately far above any entitlement: this rule guards against nonsense input
    /// (a ten-year request), not against an insufficient balance. A limit near the quota
    /// would swallow the "not enough days left" rule instead of complementing it.
    /// </summary>
    public const int MaxLeaveDays = 365;

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

        // hung on EndDate rather than on the command itself: RuleFor(x => x) leaves
        // PropertyName empty and the client gets an error under the key ""
        RuleFor(x => x.EndDate)
            .Must((command, endDate) => endDate.DayNumber - command.StartDate.DayNumber + 1 <= MaxLeaveDays)
            .WithMessage($"Leave cannot exceed {MaxLeaveDays} days.");
    }
}