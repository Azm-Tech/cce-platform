namespace CCE.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core <c>DbContext</c> for application-layer use.
/// Phase 06 defines a concrete <c>CceDbContext</c> in <c>CCE.Infrastructure</c> that implements
/// this interface and adds the real <c>DbSet&lt;T&gt;</c> properties. Foundation ships only
/// the interface contract — so far just <see cref="SaveChangesAsync"/>.
/// </summary>
public interface ICceDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
