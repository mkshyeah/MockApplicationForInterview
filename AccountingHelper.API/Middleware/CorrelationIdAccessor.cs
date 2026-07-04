using AccountingHelper.Domain.Interfaces;

namespace AccountingHelper.API.Middleware;

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = string.Empty;
}