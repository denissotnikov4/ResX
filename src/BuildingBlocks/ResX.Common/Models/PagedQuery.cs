namespace ResX.Common.Models;

public abstract record PagedQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    protected PagedQuery() { }
    protected PagedQuery(int pageNumber, int pageSize)
    {
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
    }
}
