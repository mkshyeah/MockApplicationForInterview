using System.Windows.Input;

namespace AccountingHelper.Contracts.Messaging;

public sealed record PayrollRunCompleted(Guid PayrollRunId, int ProcessedCount, int SkippedCount);
