namespace CoreBanking.API.Models
{
    public class PaginationResponse<TEntity>(int index, int pageSize, long count, IEnumerable<TEntity> items)
    {
        public int Index { get => index; }
        public int PageSize { get => pageSize; }
        public long TotalCount { get => count; }
        public IEnumerable<TEntity> Items { get => items; }
    }
}
