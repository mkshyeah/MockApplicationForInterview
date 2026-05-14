namespace AccountingHelper.Application.DTOs.Responses;

public class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Offset { get; init; }
    public int Limit { get; init; }
    public int Returned => Items.Count;

    public static PagedResponse<T> Create(IReadOnlyCollection<T> items, int total, int limit, int offset)
    {
        return new PagedResponse<T>
        {
            Items = items,
            Total = total,
            Offset = offset,
            Limit = limit
        };
    }
}