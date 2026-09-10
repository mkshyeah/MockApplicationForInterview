namespace AccountingHelper.Contracts.Messaging;

public sealed record SalaryChanged(Guid EmployeeId, Guid SalaryId);