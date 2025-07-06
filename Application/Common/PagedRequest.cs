namespace Application.Common;

public record PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public PagedRequest() { }
    public PagedRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
	}
}
