namespace BuildingBlocks.Pagination;

public static class Pagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 30;
    public const int MaxPageSize = 200;

    public static (int Page, int PageSize, int Skip) Normalize(int? page, int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? DefaultPage : page.Value;
        var normalizedPageSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };

        return (normalizedPage, normalizedPageSize, (normalizedPage - 1) * normalizedPageSize);
    }
}

public sealed record PaginationMeta(int Page, int PageSize, int TotalCount);

public sealed record PagedListResponse<T>(
    IReadOnlyList<T> Items,
    PaginationMeta Pagination);
