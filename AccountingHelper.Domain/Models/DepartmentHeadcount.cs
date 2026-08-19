namespace AccountingHelper.Domain.Models;

public sealed record DepartmentHeadcount(Guid DepartmentId, string DepartmentName, int Headcount);