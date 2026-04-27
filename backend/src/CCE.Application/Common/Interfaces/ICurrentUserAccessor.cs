namespace CCE.Application.Common.Interfaces;

/// <summary>
/// Provides the actor identifier of the current request for use by audit / domain logic.
/// Returns <c>"system"</c> for background jobs and seeders. Implementations live in:
/// - <c>CCE.Api.Internal</c> / <c>CCE.Api.External</c> — HttpContext-based.
/// - <c>CCE.Infrastructure.Tests</c> — fake.
/// - Seeder CLI — fixed <c>"seeder"</c>.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Stable, audit-friendly actor string. Common values:
    /// <c>"user:{guid}"</c>, <c>"upn:{email}"</c>, <c>"system"</c>, <c>"seeder"</c>.
    /// </summary>
    string GetActor();

    /// <summary>Optional correlation id (e.g., trace id). Returns <see cref="System.Guid.Empty"/> when none.</summary>
    System.Guid GetCorrelationId();
}
