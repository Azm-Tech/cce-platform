using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Common.Pagination;

public static class SoftDeleteExtensions
{
    /// <summary>
    /// Strips EF's global query filters (e.g., soft-delete) when the source is an EF queryable.
    /// In-memory test queryables (which already have no filters) pass through unchanged.
    /// </summary>
    public static IQueryable<T> WithoutSoftDeleteFilter<T>(this IQueryable<T> query) where T : class =>
        query is IAsyncEnumerable<T> ? query.IgnoreQueryFilters() : query;
}
