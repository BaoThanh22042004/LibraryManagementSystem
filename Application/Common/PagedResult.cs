namespace Application.Common;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Count { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Count / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

	public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int count, int page, int pageSize)
    {
        Items = items;
        this.Count = count;
        Page = page;
        PageSize = pageSize;
    }
}
