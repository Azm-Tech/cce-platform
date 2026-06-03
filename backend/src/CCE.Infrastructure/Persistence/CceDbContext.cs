using System.Linq;
using System.Linq.Expressions;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Evaluation;
using CCE.Domain.Identity;
using CCE.Domain.InteractiveCity;
using CCE.Domain.KnowledgeMaps;
using CCE.Domain.Lookups;
using CCE.Domain.Media;
using CCE.Domain.Notifications;
using CCE.Domain.PlatformSettings;
using CCE.Domain.Surveys;
using CCE.Domain.Verification;
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
    public DbSet<ExpertRequestAttachment> ExpertRequestAttachments => Set<ExpertRequestAttachment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ─── Content ───
    public DbSet<AssetFile> AssetFiles => Set<AssetFile>();
    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<News> News => Set<News>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<HomepageSection> HomepageSections => Set<HomepageSection>();
    public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();

    // ─── Country ───
    public DbSet<CCE.Domain.Country.Country> Countries => Set<CCE.Domain.Country.Country>();
    public DbSet<CountryProfile> CountryProfiles => Set<CountryProfile>();
    public DbSet<CountryContentRequest> CountryContentRequests => Set<CountryContentRequest>();
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
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<UserNotificationSettings> UserNotificationSettings => Set<UserNotificationSettings>();

    // ─── Verification ───
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<UserVerification> UserVerifications => Set<UserVerification>();

    // ─── Surveys ───
    public DbSet<ServiceRating> ServiceRatings => Set<ServiceRating>();
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

    // ─── Evaluation ───
    public DbSet<ServiceEvaluation> ServiceEvaluations => Set<ServiceEvaluation>();

    // ─── Media ───
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    // ─── Platform Settings ───
    public DbSet<HomepageSettings> HomepageSettings => Set<HomepageSettings>();
    public DbSet<HomepageCountry> HomepageCountries => Set<HomepageCountry>();
    public DbSet<AboutSettings> AboutSettings => Set<AboutSettings>();
    public DbSet<GlossaryEntry> GlossaryEntries => Set<GlossaryEntry>();
    public DbSet<PoliciesSettings> PoliciesSettings => Set<PoliciesSettings>();
    public DbSet<KnowledgePartner> KnowledgePartners => Set<KnowledgePartner>();
    public DbSet<PolicySection> PolicySections => Set<PolicySection>();

    // ─── Lookups ───
    public DbSet<CountryCode> CountryCodes => Set<CountryCode>();

    // ─── ICceDbContext (read-only queryables — no tracking) ───
    IQueryable<User> ICceDbContext.Users => Users.AsNoTracking();
    IQueryable<Role> ICceDbContext.Roles => Roles.AsNoTracking();
    IQueryable<IdentityUserRole<System.Guid>> ICceDbContext.UserRoles => UserRoles.AsNoTracking();
    IQueryable<StateRepresentativeAssignment> ICceDbContext.StateRepresentativeAssignments => StateRepresentativeAssignments.AsNoTracking();
    IQueryable<CCE.Domain.Country.Country> ICceDbContext.Countries => Countries.AsNoTracking();
    IQueryable<ExpertRegistrationRequest> ICceDbContext.ExpertRegistrationRequests => ExpertRegistrationRequests.AsNoTracking();
    IQueryable<ExpertRequestAttachment> ICceDbContext.ExpertRequestAttachments => ExpertRequestAttachments.AsNoTracking();
    IQueryable<ExpertProfile> ICceDbContext.ExpertProfiles => ExpertProfiles.AsNoTracking();
    IQueryable<RefreshToken> ICceDbContext.RefreshTokens => RefreshTokens.AsNoTracking();
    IQueryable<AssetFile> ICceDbContext.AssetFiles => AssetFiles.AsNoTracking();
    IQueryable<ResourceCategory> ICceDbContext.ResourceCategories => ResourceCategories.AsNoTracking();
    IQueryable<CCE.Domain.Content.Resource> ICceDbContext.Resources => Resources.AsNoTracking();
    IQueryable<CountryContentRequest> ICceDbContext.CountryContentRequests => CountryContentRequests.AsNoTracking();
    IQueryable<CountryProfile> ICceDbContext.CountryProfiles => CountryProfiles.AsNoTracking();
    IQueryable<CountryKapsarcSnapshot> ICceDbContext.CountryKapsarcSnapshots => CountryKapsarcSnapshots.AsNoTracking();
    IQueryable<CCE.Domain.Content.News> ICceDbContext.News => News.AsNoTracking();
    IQueryable<CCE.Domain.Content.Event> ICceDbContext.Events => Events.AsNoTracking();
    IQueryable<CCE.Domain.Content.Tag> ICceDbContext.Tags => Tags.AsNoTracking();
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
    IQueryable<NotificationLog> ICceDbContext.NotificationLogs => NotificationLogs.AsNoTracking();
    IQueryable<UserNotificationSettings> ICceDbContext.UserNotificationSettings => UserNotificationSettings.AsNoTracking();
    IQueryable<ServiceRating> ICceDbContext.ServiceRatings => ServiceRatings.AsNoTracking();
    IQueryable<AuditEvent> ICceDbContext.AuditEvents => AuditEvents.AsNoTracking();
    IQueryable<KnowledgeMap> ICceDbContext.KnowledgeMaps => KnowledgeMaps.AsNoTracking();
    IQueryable<KnowledgeMapNode> ICceDbContext.KnowledgeMapNodes => KnowledgeMapNodes.AsNoTracking();
    IQueryable<KnowledgeMapEdge> ICceDbContext.KnowledgeMapEdges => KnowledgeMapEdges.AsNoTracking();
    IQueryable<KnowledgeMapAssociation> ICceDbContext.KnowledgeMapAssociations => KnowledgeMapAssociations.AsNoTracking();
    IQueryable<CityScenario> ICceDbContext.CityScenarios => CityScenarios.AsNoTracking();
    IQueryable<CityTechnology> ICceDbContext.CityTechnologies => CityTechnologies.AsNoTracking();
    IQueryable<CityScenarioResult> ICceDbContext.CityScenarioResults => CityScenarioResults.AsNoTracking();
    IQueryable<HomepageSettings> ICceDbContext.HomepageSettings => HomepageSettings.AsNoTracking();
    IQueryable<HomepageCountry> ICceDbContext.HomepageCountries => HomepageCountries.AsNoTracking();
    IQueryable<AboutSettings> ICceDbContext.AboutSettings => AboutSettings.AsNoTracking();
    IQueryable<GlossaryEntry> ICceDbContext.GlossaryEntries => GlossaryEntries.AsNoTracking();
    IQueryable<PoliciesSettings> ICceDbContext.PoliciesSettings => PoliciesSettings.AsNoTracking();
    IQueryable<KnowledgePartner> ICceDbContext.KnowledgePartners => KnowledgePartners.AsNoTracking();
    IQueryable<PolicySection> ICceDbContext.PolicySections => PolicySections.AsNoTracking();
    IQueryable<CountryCode> ICceDbContext.CountryCodes => CountryCodes.AsNoTracking();
    IQueryable<OtpVerification> ICceDbContext.OtpVerifications => OtpVerifications.AsNoTracking();
    IQueryable<UserVerification> ICceDbContext.UserVerifications => UserVerifications.AsNoTracking();
    IQueryable<ServiceEvaluation> ICceDbContext.ServiceEvaluations => ServiceEvaluations.AsNoTracking();
    IQueryable<MediaFile> ICceDbContext.MediaFiles => MediaFiles.AsNoTracking();

    void ICceDbContext.Add<T>(T entity) where T : class => Set<T>().Add(entity);
    void ICceDbContext.Attach<T>(T entity) where T : class => Set<T>().Attach(entity);
    void ICceDbContext.Delete<T>(T entity) where T : class => Set<T>().Remove(entity);
    void ICceDbContext.DeleteRange<T>(System.Collections.Generic.IEnumerable<T> entities) where T : class
        => Set<T>().RemoveRange(entities);

    void ICceDbContext.SetExpectedRowVersion<T>(T entity, byte[] expectedRowVersion) where T : class
        => this.SetExpectedRowVersion(entity, expectedRowVersion);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrent update conflict.", ex);
        }
    }

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
