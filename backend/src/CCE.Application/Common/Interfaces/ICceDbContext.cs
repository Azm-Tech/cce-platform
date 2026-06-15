using System.Linq;
using CCE.Domain.Audit;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Domain.Evaluation;
using CCE.Domain.Identity;
using CCE.Domain.InteractiveCity;
using CCE.Domain.InteractiveMaps;
using CCE.Domain.KnowledgeMaps;
using CCE.Domain.Lookups;
using CCE.Domain.Media;
using CCE.Domain.Notifications;
using CCE.Domain.PlatformSettings;
using CCE.Domain.Surveys;
using CCE.Domain.Verification;
using Microsoft.AspNetCore.Identity;
using DomainCountry = CCE.Domain.Country;

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
    IQueryable<DomainCountry.Country> Countries { get; }
    IQueryable<ExpertRegistrationRequest> ExpertRegistrationRequests { get; }
    IQueryable<ExpertRequestAttachment> ExpertRequestAttachments { get; }
    IQueryable<ExpertProfile> ExpertProfiles { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<AssetFile> AssetFiles { get; }
    IQueryable<ResourceCategory> ResourceCategories { get; }
    IQueryable<Resource> Resources { get; }
    IQueryable<DomainCountry.CountryContentRequest> CountryContentRequests { get; }
    IQueryable<DomainCountry.CountryProfile> CountryProfiles { get; }
    IQueryable<DomainCountry.CountryKapsarcSnapshot> CountryKapsarcSnapshots { get; }
    IQueryable<News> News { get; }
    IQueryable<Event> Events { get; }
    IQueryable<Tag> Tags { get; }
    IQueryable<Page> Pages { get; }
    IQueryable<HomepageSection> HomepageSections { get; }
    IQueryable<Topic> Topics { get; }
    IQueryable<Post> Posts { get; }
    IQueryable<PostReply> PostReplies { get; }
    IQueryable<PostVote> PostVotes { get; }
    IQueryable<ReplyVote> ReplyVotes { get; }
    IQueryable<PostAttachment> PostAttachments { get; }
    IQueryable<Mention> Mentions { get; }
    IQueryable<Poll> Polls { get; }
    IQueryable<PollOption> PollOptions { get; }
    IQueryable<PollVote> PollVotes { get; }
    IQueryable<TopicFollow> TopicFollows { get; }
    IQueryable<UserFollow> UserFollows { get; }
    IQueryable<PostFollow> PostFollows { get; }
    IQueryable<CCE.Domain.Community.Community> Communities { get; }
    IQueryable<CommunityMembership> CommunityMemberships { get; }
    IQueryable<CommunityJoinRequest> CommunityJoinRequests { get; }
    IQueryable<CommunityFollow> CommunityFollows { get; }
    IQueryable<NotificationTemplate> NotificationTemplates { get; }
    IQueryable<UserNotification> UserNotifications { get; }
    IQueryable<NotificationLog> NotificationLogs { get; }
    IQueryable<UserNotificationSettings> UserNotificationSettings { get; }
    IQueryable<ServiceRating> ServiceRatings { get; }
    IQueryable<AuditEvent> AuditEvents { get; }
    IQueryable<KnowledgeMap> KnowledgeMaps { get; }
    IQueryable<KnowledgeMapNode> KnowledgeMapNodes { get; }
    IQueryable<KnowledgeMapEdge> KnowledgeMapEdges { get; }
    IQueryable<KnowledgeMapAssociation> KnowledgeMapAssociations { get; }
    IQueryable<CityScenario> CityScenarios { get; }
    IQueryable<CityTechnology> CityTechnologies { get; }
    IQueryable<CityScenarioResult> CityScenarioResults { get; }
    IQueryable<HomepageSettings> HomepageSettings { get; }
    IQueryable<HomepageCountry> HomepageCountries { get; }
    IQueryable<AboutSettings> AboutSettings { get; }
    IQueryable<GlossaryEntry> GlossaryEntries { get; }
    IQueryable<PoliciesSettings> PoliciesSettings { get; }
    IQueryable<KnowledgePartner> KnowledgePartners { get; }
    IQueryable<PolicySection> PolicySections { get; }

    // ─── Lookups ───
    IQueryable<CountryCode> CountryCodes { get; }

    // ─── Verification ───
    IQueryable<OtpVerification> OtpVerifications { get; }
    IQueryable<UserVerification> UserVerifications { get; }

    // ─── Evaluation ───
    IQueryable<ServiceEvaluation> ServiceEvaluations { get; }

    // ─── Media ───
    IQueryable<MediaFile> MediaFiles { get; }

    // ─── Interactive Maps ───
    IQueryable<InteractiveMap> InteractiveMaps { get; }
    IQueryable<InteractiveMapNode> InteractiveMapNodes { get; }

    // ─── Interest Topics ───
    IQueryable<InterestTopic> InterestTopics { get; }

    // Write operations
    void Add<T>(T entity) where T : class;
    void Attach<T>(T entity) where T : class;
    void Delete<T>(T entity) where T : class;
    void DeleteRange<T>(System.Collections.Generic.IEnumerable<T> entities) where T : class;

    void SetExpectedRowVersion<T>(T entity, byte[] expectedRowVersion) where T : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
