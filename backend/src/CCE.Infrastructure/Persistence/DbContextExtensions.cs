using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

internal static class DbContextExtensions
{
    /// <summary>
    /// Sets the expected RowVersion for optimistic concurrency on a tracked entity.
    /// </summary>
    public static void SetExpectedRowVersion<T>(
        this DbContext db, T entity, byte[] expectedRowVersion)
        where T : class
    {
        db.Entry(entity).OriginalValues["RowVersion"] = expectedRowVersion;
    }
}
