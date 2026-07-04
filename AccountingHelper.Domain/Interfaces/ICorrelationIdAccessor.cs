namespace AccountingHelper.Domain.Interfaces;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}