using System.Linq;
using System.Linq.Expressions;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Identity;
using CCE.Domain.InteractiveCity;
using CCE.Domain.KnowledgeMaps;
using CCE.Domain.Notifications;
using CCE.Domain.Surveys;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Application <see cref="DbContext"/>. Extends ASP.NET Identity's
/// <see cref="IdentityDbContext{User, Role, Guid}"/> so the Identity tables
/// (AspNetUsers/Roles/UserRoles/etc.) coexist with CCE entity tables in one model.
/// Snake-case naming via <c>EFCore.NamingConventions</c> applied in <c>DependencyInjection</c>.
/// </summary>
public sealed class CceDbContext
    : IdentityDbContext<User, Role, System.Guid>, ICceDbContext
{
    public CceDbContext(DbContextOptions<CceDbContext> options) : base(options) { }

    // ─── Audit (Foundation) ───
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    // ─── Identity bounded context ───
    public DbSet<StateRepresentativeAssignment> StateRepresentativeAssignments => Set<StateRepresentativeAssignment>();
    public DbSet<ExpertProfile> ExpertProfiles => Set<ExpertProfile>();
    public DbSet<ExpertRegistrationRequest> ExpertRegistrationRequests => Set<ExpertRegistrationRequest>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ─── Content ───
    public DbSet<AssetFile> AssetFiles => Set<AssetFile>();
    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<News> News => Set<News>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<HomepageSection> HomepageSections => Set<HomepageSection>();
    public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();

    // ─── Country ───
    public DbSet<CCE.Domain.Country.Country> Countries => Set<CCE.Domain.Country.Country>();
    public DbSet<CountryProfile> CountryProfiles => Set<CountryProfile>();
    public DbSet<CountryResourceRequest> CountryResourceRequests => Set<CountryResourceRequest>();
    public DbSet<CountryKapsarcSnapshot> CountryKapsarcSnapshots => Set<CountryKapsarcSnapshot>();

    // ─── Community ───
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostReply> PostReplies => Set<PostReply>();
    public DbSet<PostRating> PostRatings => Set<PostRating>();
    public DbSet<TopicFollow> TopicFollows => Set<TopicFollow>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<PostFollow> PostFollows => Set<PostFollow>();

    // ─── Knowledge Maps ───
    public DbSet<KnowledgeMap> KnowledgeMaps => Set<KnowledgeMap>();
    public DbSet<KnowledgeMapNode> KnowledgeMapNodes => Set<KnowledgeMapNode>();
    public DbSet<KnowledgeMapEdge> KnowledgeMapEdges => Set<KnowledgeMapEdge>();
    public DbSet<KnowledgeMapAssociation> KnowledgeMapAssociations => Set<KnowledgeMapAssociation>();

    // ─── Interactive City ───
    public DbSet<CityScenario> CityScenarios => Set<CityScenario>();
    public DbSet<CityTechnology> CityTechnologies => Set<CityTechnology>();
    public DbSet<CityScenarioResult> CityScenarioResults => Set<CityScenarioResult>();

    // ─── Notifications ───
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    // ─── Surveys ───
    public DbSet<ServiceRating> ServiceRatings => Set<ServiceRating>();
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

    // ─── ICceDbContext (read-only queryables — no tracking) ───
    IQueryable<User> ICceDbContext.Users => Users.AsNoTracking();
    IQueryable<Role> ICceDbContext.Roles => Roles.AsNoTracking();
    IQueryable<IdentityUserRole<System.Guid>> ICceDbContext.UserRoles => UserRoles.AsNoTracking();
    IQueryable<StateRepresentativeAssignment> ICceDbContext.StateRepresentativeAssignments => StateRepresentativeAssignments.AsNoTracking();
    IQueryable<CCE.Domain.Country.Country> ICceDbContext.Countries => Countries.AsNoTracking();
    IQueryable<ExpertRegistrationRequest> ICceDbContext.ExpertRegistrationRequests => ExpertRegistrationRequests.AsNoTracking();
    IQueryable<ExpertProfile> ICceDbContext.ExpertProfiles => ExpertProfiles.AsNoTracking();
    IQueryable<RefreshToken> ICceDbContext.RefreshTokens => RefreshTokens.AsNoTracking();
    IQueryable<AssetFile> ICceDbContext.AssetFiles => AssetFiles.AsNoTracking();
    IQueryable<ResourceCategory> ICceDbContext.ResourceCategories => ResourceCategories.AsNoTracking();
    IQueryable<CCE.Domain.Content.Resource> ICceDbContext.Resources => Resources.AsNoTracking();
    IQueryable<CountryResourceRequest> ICceDbContext.CountryResourceRequests => CountryResourceRequests.AsNoTracking();
    IQueryable<CountryProfile> ICceDbContext.CountryProfiles => CountryProfiles.AsNoTracking();
    IQueryable<CountryKapsarcSnapshot> ICceDbContext.CountryKapsarcSnapshots => CountryKapsarcSnapshots.AsNoTracking();
    IQueryable<CCE.Domain.Content.News> ICceDbContext.News => News.AsNoTracking();
    IQueryable<CCE.Domain.Content.Event> ICceDbContext.Events => Events.AsNoTracking();
    IQueryable<CCE.Domain.Content.Page> ICceDbContext.Pages => Pages.AsNoTracking();
    IQueryable<HomepageSection> ICceDbContext.HomepageSections => HomepageSections.AsNoTracking();
    IQueryable<Topic> ICceDbContext.Topics => Topics.AsNoTracking();
    IQueryable<Post> ICceDbContext.Posts => Posts.AsNoTracking();
    IQueryable<PostReply> ICceDbContext.PostReplies => PostReplies.AsNoTracking();
    IQueryable<PostRating> ICceDbContext.PostRatings => PostRatings.AsNoTracking();
    IQueryable<TopicFollow> ICceDbContext.TopicFollows => TopicFollows.AsNoTracking();
    IQueryable<UserFollow> ICceDbContext.UserFollows => UserFollows.AsNoTracking();
    IQueryable<PostFollow> ICceDbContext.PostFollows => PostFollows.AsNoTracking();
    IQueryable<NotificationTemplate> ICceDbContext.NotificationTemplates => NotificationTemplates.AsNoTracking();
    IQueryable<UserNotification> ICceDbContext.UserNotifications => UserNotifications.AsNoTracking();
    IQueryable<ServiceRating> ICceDbContext.ServiceRatings => ServiceRatings.AsNoTracking();
    IQueryable<AuditEvent> ICceDbContext.AuditEvents => AuditEvents.AsNoTracking();
    IQueryable<KnowledgeMap> ICceDbContext.KnowledgeMaps => KnowledgeMaps.AsNoTracking();
    IQueryable<KnowledgeMapNode> ICceDbContext.KnowledgeMapNodes => KnowledgeMapNodes.AsNoTracking();
    IQueryable<KnowledgeMapEdge> ICceDbContext.KnowledgeMapEdges => KnowledgeMapEdges.AsNoTracking();
    IQueryable<KnowledgeMapAssociation> ICceDbContext.KnowledgeMapAssociations => KnowledgeMapAssociations.AsNoTracking();
    IQueryable<CityScenario> ICceDbContext.CityScenarios => CityScenarios.AsNoTracking();
    IQueryable<CityTechnology> ICceDbContext.CityTechnologies => CityTechnologies.AsNoTracking();
    IQueryable<CityScenarioResult> ICceDbContext.CityScenarioResults => CityScenarioResults.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CceDbContext).Assembly);
        ApplySoftDeleteFilter(builder);
    }

    /// <summary>
    /// Spec §5.5: every <see cref="ISoftDeletable"/> entity gets a global query filter
    /// <c>HasQueryFilter(e =&gt; !e.IsDeleted)</c>. To bypass, use <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        var isDeletedProperty = typeof(ISoftDeletable).GetProperty(nameof(ISoftDeletable.IsDeleted))!;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(Expression.Property(parameter, isDeletedProperty));
            var lambda = Expression.Lambda(body, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
