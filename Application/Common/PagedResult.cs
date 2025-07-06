namespace Application.Common;

public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Count { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
	public int TotalPages => (int)Math.Ceiling((double)Count / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

	public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int count, int page, int pageSize)
    {
        Items = items;
        Count = count;
        Page = page;
        PageSize = pageSize;
    }
}
