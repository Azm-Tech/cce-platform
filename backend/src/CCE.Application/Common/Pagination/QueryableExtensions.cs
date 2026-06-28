using System.Linq.Expressions;

namespace CCE.Application.Common.Pagination;

public static class QueryableExtensions
{
    /// <summary>
    /// Conditionally appends a Where clause. When <paramref name="condition"/> is false
    /// the original query is returned unmodified.
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;
}
