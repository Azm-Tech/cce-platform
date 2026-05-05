using CCE.Application.Assistant;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Community;
using CCE.Application.Content;
using CCE.Application.Content.Public;
using CCE.Application.Country;
using CCE.Application.Identity;
using CCE.Application.Identity.Public;
using CCE.Application.InteractiveCity;
using CCE.Application.Notifications;
using CCE.Application.Notifications.Public;
using CCE.Application.Reports;
using CCE.Application.Search;
using CCE.Application.Surveys;
using CCE.Infrastructure.Assistant;
using CCE.Infrastructure.Community;
using CCE.Infrastructure.Content;
using CCE.Infrastructure.InteractiveCity;
using CCE.Infrastructure.Sanitization;
using CCE.Infrastructure.Country;
using CCE.Infrastructure.Notifications;
using CCE.Infrastructure.Reports;
using CCE.Infrastructure.Surveys;
using CCE.Domain.Common;
using CCE.Infrastructure.Email;
using CCE.Infrastructure.Files;
using CCE.Infrastructure.Identity;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Interceptors;
using CCE.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CCE.Infrastructure;

/// <summary>
/// Composition-root extension methods for the Infrastructure layer.
/// Web APIs call <see cref="AddInfrastructure"/> from <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CceInfrastructureOptions>()
            .Bind(configuration.GetSection(CceInfrastructureOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Clock
        services.AddSingleton<ISystemClock, SystemClock>();

        // Default current-user accessor — API hosts override with HttpContext-based impl.
        services.TryAddScoped<ICurrentUserAccessor, SystemCurrentUserAccessor>();

        // Default country-scope accessor — API hosts override with HttpContext-based impl.
        services.TryAddScoped<ICountryScopeAccessor, SystemCountryScopeAccessor>();

        // Interceptors
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<DomainEventDispatcher>();

        // EF Core — SQL Server with snake_case naming + audit + domain-event interceptors
        services.AddDbContext<CceDbContext>((sp, opts) =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            opts.UseSqlServer(infraOpts.SqlConnectionString);
            opts.UseSnakeCaseNamingConvention();
            opts.AddInterceptors(
                sp.GetRequiredService<AuditingInterceptor>(),
                sp.GetRequiredService<DomainEventDispatcher>());
        });
        services.AddScoped<ICceDbContext>(sp => sp.GetRequiredService<CceDbContext>());
        services.AddScoped<IUserSyncService, UserSyncService>();
        services.AddScoped<IUserRoleAssignmentService, UserRoleAssignmentService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        // Sub-11 Phase 01 — Microsoft Graph user-create + CCE-side persist.
        // Factory is singleton (ClientSecretCredential is thread-safe and reusable);
        // service is scoped because it consumes the scoped CceDbContext.
        services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));
        services.AddSingleton<EntraIdGraphClientFactory>();
        services.AddScoped<EntraIdRegistrationService>();
        services.AddScoped<EntraIdUserSyncService>();

        // Sub-11d — outbound email transport. SMTP-backed when
        // Email:Provider=smtp; otherwise NullEmailSender (logs + discards).
        // Singleton because both impls are stateless + thread-safe.
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailOptions>>();
            var provider = (opts.Value.Provider ?? "null").ToLowerInvariant();
            return provider switch
            {
                "smtp" => ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp),
                _      => ActivatorUtilities.CreateInstance<NullEmailSender>(sp),
            };
        });
        services.AddScoped<IStateRepAssignmentService, StateRepAssignmentService>();
        services.AddScoped<IExpertWorkflowService, ExpertWorkflowService>();
        services.AddScoped<IExpertRequestSubmissionService, ExpertRequestSubmissionService>();

        // File storage + virus scanning
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddTransient<IClamAvScanner, ClamAvScanner>();
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizerWrapper>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IResourceCategoryService, ResourceCategoryService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<IHomepageSectionService, HomepageSectionService>();
        services.AddScoped<ICountryResourceRequestService, CountryResourceRequestService>();
        services.AddScoped<IResourceViewCountService, ResourceViewCountService>();
        services.AddScoped<ICountryAdminService, CountryAdminService>();
        services.AddScoped<ICountryProfileService, CountryProfileService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<ICommunityModerationService, CommunityModerationService>();
        services.AddScoped<ICommunityWriteService, CommunityWriteService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<IUserNotificationService, UserNotificationService>();
        services.AddScoped<IUserRegistrationsReportService, UserRegistrationsReportService>();
        services.AddScoped<IExpertReportService, ExpertReportService>();
        services.AddScoped<ISatisfactionSurveyReportService, SatisfactionSurveyReportService>();
        services.AddScoped<ICommunityPostReportService, CommunityPostReportService>();
        services.AddScoped<INewsReportService, NewsReportService>();
        services.AddScoped<IEventReportService, EventReportService>();
        services.AddScoped<IResourceReportService, ResourceReportService>();
        services.AddScoped<ICountryProfilesReportService, CountryProfilesReportService>();

        // Surveys
        services.AddScoped<IServiceRatingService, ServiceRatingService>();

        // Smart assistant — factory routes to stub or Anthropic based on
        // Assistant:Provider config + ANTHROPIC_API_KEY env-var (Sub-10a).
        services.AddScoped<ICitationSearch, CitationSearch>();
        services.AddCceAssistantClient(configuration);

        // Interactive City
        services.AddScoped<ICityScenarioService, CityScenarioService>();

        // Search
        services.AddScoped<ISearchClient, MeilisearchClient>();
        services.AddScoped<ISearchQueryLogger, SearchQueryLogger>();

        // Redis — singleton multiplexer.
        // AbortOnConnectFail=false: Connect returns a degraded multiplexer instead of throwing
        // when the server is unreachable at startup. This lets the host start cleanly even with
        // a bad connection string, so the /health/ready health-check can probe Redis lazily
        // and return 503 (degraded) as expected, rather than crashing the host.
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            var config = ConfigurationOptions.Parse(infraOpts.RedisConnectionString);
            config.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(config);
        });

        return services;
    }

    /// <summary>
    /// Fallback <see cref="ICurrentUserAccessor"/> for non-HTTP contexts (seeders, background jobs).
    /// API hosts register an HttpContext-based implementation that overrides this.
    /// </summary>
    private sealed class SystemCurrentUserAccessor : ICurrentUserAccessor
    {
        public string GetActor() => "system";
        public System.Guid GetCorrelationId() => System.Guid.Empty;
        public System.Guid? GetUserId() => null;
    }
}
