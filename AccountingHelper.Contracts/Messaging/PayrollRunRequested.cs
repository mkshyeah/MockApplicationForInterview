namespace AccountingHelper.Contracts.Messaging;

public sealed record PayrollRunRequested(Guid PayrollRunId, int Year, int Month);