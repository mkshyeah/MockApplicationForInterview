namespace AccountingHelper.Contracts.Messaging;

public sealed record ImportCompleted(Guid ImportJobId, int ImportedCount, int FailedCount);