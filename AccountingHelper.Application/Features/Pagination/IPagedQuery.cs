namespace AccountingHelper.Application.Features.Pagination;

public interface IPagedQuery
{
    int Offset { get; }
    int Limit { get; }
}