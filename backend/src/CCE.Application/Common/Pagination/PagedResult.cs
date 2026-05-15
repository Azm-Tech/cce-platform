using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Common.Pagination;

/// <summary>
/// Page of <typeparamref name="T"/> entries plus the total count for the unpaged query.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total)
{
    /// <summary>
    /// Projects each item into a new shape while preserving pagination metadata.
    /// </summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, Total);
}

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

    /// <summary>
    /// Paginates and projects in a single query — SQL only fetches DTO columns.
    /// Use for list endpoints where you don't need the full entity.
    /// </summary>
    public static async Task<PagedResult<TDto>> ToPagedResultAsync<T, TDto>(
        this IQueryable<T> query,
        Expression<Func<T, TDto>> projection,
        int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = query is IAsyncEnumerable<T>
            ? await query.LongCountAsync(ct).ConfigureAwait(false)
            : query.LongCount();

        var projected = query.Select(projection);
        var items = projected is IAsyncEnumerable<TDto>
            ? await projected.Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct).ConfigureAwait(false)
            : projected.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<TDto>(items, page, pageSize, total);
    }

    /// <summary>
    /// Materialises an <see cref="IQueryable{T}"/> as a list, dispatching to EF's
    /// <c>ToListAsync</c> when the query implements <see cref="IAsyncEnumerable{T}"/>
    /// and falling back to plain <c>ToList</c> for in-memory test queryables.
    /// </summary>
    public static async Task<List<T>> ToListAsyncEither<T>(this IQueryable<T> query, CancellationToken ct)
    {
        return query is IAsyncEnumerable<T>
            ? await query.ToListAsync(ct).ConfigureAwait(false)
            : query.ToList();
    }

    /// <summary>
    /// Counts the elements of an <see cref="IQueryable{T}"/>, dispatching to EF's
    /// <c>CountAsync</c> when the query implements <see cref="IAsyncEnumerable{T}"/>
    /// and falling back to plain <c>Count</c> for in-memory test queryables.
    /// </summary>
    public static async Task<int> CountAsyncEither<T>(this IQueryable<T> query, CancellationToken ct)
        => query is IAsyncEnumerable<T>
            ? await query.CountAsync(ct).ConfigureAwait(false)
            : query.Count();
}
