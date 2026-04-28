using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Common.Pagination;

/// <summary>
/// Page of <typeparamref name="T"/> entries plus the total count for the unpaged query.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);

public static class PaginationExtensions
{
    public const int MaxPageSize = 100;

    /// <summary>
    /// Materialises an <see cref="IQueryable{T}"/> as a <see cref="PagedResult{T}"/>.
    /// <c>page</c> is 1-based, clamped to <c>&gt;= 1</c>. <c>pageSize</c> is clamped to <c>[1, 100]</c>.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var total = query is IAsyncEnumerable<T>
            ? await query.LongCountAsync(ct).ConfigureAwait(false)
            : query.LongCount();
        var items = query is IAsyncEnumerable<T>
            ? await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct).ConfigureAwait(false)
            : query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>(items, page, pageSize, total);
    }
}
