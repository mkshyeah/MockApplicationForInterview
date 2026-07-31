using AccountingHelper.Domain.Enums;

namespace AccountingHelper.Domain.Models;

public static class LeaveEntitlement
{
    public static int DaysFor(LeaveType type) => type switch
    {
        LeaveType.Annual => 28,
        LeaveType.Sick => 14,
        LeaveType.Unpaid => 30,
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"No entitlement configured for {type}.")
    };
}