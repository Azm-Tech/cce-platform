using CCE.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Maps SQL Server exceptions surfaced by EF Core into domain exceptions, so the
/// application/UI layer doesn't need to know SQL error numbers.
/// Recognized:
/// - 2601 / 2627 → <see cref="DuplicateException"/>
/// - <see cref="DbUpdateConcurrencyException"/> → <see cref="ConcurrencyException"/>
/// - everything else → rethrown unchanged.
/// </summary>
public static class DbExceptionMapper
{
    public const int SqlUniqueConstraintViolation = 2627;
    public const int SqlUniqueIndexViolation = 2601;

    public static System.Exception Map(System.Exception ex)
    {
        if (ex is DbUpdateConcurrencyException concurrency)
        {
            return new ConcurrencyException("Concurrent update conflict.", concurrency);
        }
        if (ex is DbUpdateException dbUpdate
            && dbUpdate.InnerException is SqlException sqlInner
            && (sqlInner.Number == SqlUniqueConstraintViolation
                || sqlInner.Number == SqlUniqueIndexViolation))
        {
            return new DuplicateException("Duplicate value rejected by unique index.", dbUpdate);
        }
        return ex;
    }
}
