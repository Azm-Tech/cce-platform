using System.Linq;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core <c>DbContext</c> for application-layer use.
/// Phase 06 defines a concrete <c>CceDbContext</c> in <c>CCE.Infrastructure</c> that implements
/// this interface and adds the real <c>DbSet&lt;T&gt;</c> properties. Application handlers
/// query through these <see cref="System.Linq.IQueryable{T}"/> projections so the layer stays
/// EF-agnostic at the type level (the <c>ToPagedResultAsync</c> + <c>LongCountAsync</c>
/// extensions branch on <c>IAsyncEnumerable&lt;T&gt;</c> at runtime).
/// </summary>
public interface ICceDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }
    IQueryable<IdentityUserRole<System.Guid>> UserRoles { get; }
    IQueryable<StateRepresentativeAssignment> StateRepresentativeAssignments { get; }
    IQueryable<Country> Countries { get; }
    IQueryable<ExpertRegistrationRequest> ExpertRegistrationRequests { get; }
    IQueryable<ExpertProfile> ExpertProfiles { get; }
    IQueryable<AssetFile> AssetFiles { get; }
    IQueryable<Resource> Resources { get; }
    IQueryable<CountryResourceRequest> CountryResourceRequests { get; }
    IQueryable<News> News { get; }
    IQueryable<Event> Events { get; }
    IQueryable<Page> Pages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
