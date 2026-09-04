namespace CoreBanking.API.Models;

public class PaginationResponse<TEntity>(int pageIndex, int pageSize, long count, IEnumerable<TEntity> items)
{
    public int PageIndex { get => pageIndex; }
    public int PageSize { get => pageSize; }
    public long TotalCount { get => count; }
    public IEnumerable<TEntity> Items { get => items; }
}
