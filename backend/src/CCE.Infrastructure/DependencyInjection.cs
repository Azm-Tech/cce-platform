using CCE.Application.Assistant;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Community;
using CCE.Application.Content;
using CCE.Application.Content.Public;
using CCE.Application.Evaluation;
using CCE.Application.Media;
using CCE.Application.PlatformSettings;
using CCE.Application.Country;
using CCE.Application.Identity;
using CCE.Application.Identity.Auth.Common;
using CCE.Application.Identity.Public;
using CCE.Application.InteractiveCity;
using CCE.Application.Notifications;
using CCE.Application.Notifications.Messages;
using CCE.Application.Notifications.Public;
using CCE.Application.Reports;
using CCE.Application.Search;
using CCE.Application.Surveys;
using CCE.Infrastructure.Assistant;
using CCE.Infrastructure.Community;
using CCE.Infrastructure.Content;
using CCE.Infrastructure.InteractiveCity;
using CCE.Infrastructure.InterestManagement;
using CCE.Infrastructure.Media;
using CCE.Infrastructure.Sanitization;
using CCE.Infrastructure.Country;
using CCE.Infrastructure.Notifications;
using CCE.Infrastructure.Notifications.Messaging;
using CCE.Infrastructure.Reports;
using CCE.Infrastructure.Evaluation;
using CCE.Infrastructure.Surveys;
using CCE.Application.Verification;
using CCE.Application.Localization;
using CCE.Domain.Common;
using CCE.Integration.Communication;
using CCE.Infrastructure.Email;
using CCE.Infrastructure.ExternalApis;
using CCE.Infrastructure.Files;
using CCE.Infrastructure.Identity;
using CCE.Infrastructure.Localization;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Repositories;
using CCE.Infrastructure.Security;
using CCE.Infrastructure.Persistence.Interceptors;
using CCE.Infrastructure.PlatformSettings;
using CCE.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
        IConfiguration configuration,
        bool registerConsumers = false)
    {
        services.AddOptions<CceInfrastructureOptions>()
            .Bind(configuration.GetSection(CceInfrastructureOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Clock
        services.AddSingleton<ISystemClock, SystemClock>();

        services.Configure<LocalAuthOptions>(configuration.GetSection(LocalAuthOptions.SectionName));
        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            var authOptions = configuration.GetSection(LocalAuthOptions.SectionName).Get<LocalAuthOptions>() ?? new LocalAuthOptions();
            options.TokenLifespan = TimeSpan.FromHours(Math.Max(1, authOptions.PasswordResetTokenHours));
        });

        // Localization
        services.AddSingleton<YamlLocalizationStore>();
        services.AddScoped<ILocalizationService, LocalizationService>();

        // Default current-user accessor — API hosts override with HttpContext-based impl.
        services.TryAddScoped<ICurrentUserAccessor, SystemCurrentUserAccessor>();

        // Default country-scope accessor — API hosts override with HttpContext-based impl.
        services.TryAddScoped<ICountryScopeAccessor, SystemCountryScopeAccessor>();

        // Interceptors. Registered BOTH as their concrete type (so they can be resolved directly in
        // tests) AND as IInterceptor, because the DbContext below attaches every IInterceptor via
        // sp.GetServices<IInterceptor>(). Without the IInterceptor registration these would silently
        // NOT attach — domain-event dispatch + auditing would stop. The MassTransit EF bus-outbox
        // interceptor is also registered as IInterceptor by AddEntityFrameworkOutbox, so it is picked
        // up by the same call.
        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>(
            sp => sp.GetRequiredService<AuditingInterceptor>());
        services.AddScoped<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>(
            sp => sp.GetRequiredService<DomainEventDispatcher>());

        // EF Core — SQL Server with snake_case naming + audit + domain-event interceptors
        services.AddDbContext<CceDbContext>((sp, opts) =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            opts.UseSqlServer(infraOpts.SqlConnectionString);
            opts.UseSnakeCaseNamingConvention();
            opts.AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>());
        });
        services.AddScoped<ICceDbContext>(sp => sp.GetRequiredService<CceDbContext>());

        services
            .AddIdentityCore<CCE.Domain.Identity.User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddRoles<CCE.Domain.Identity.Role>()
            .AddEntityFrameworkStores<CceDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserSyncRepository, UserSyncRepository>();
        services.AddScoped<IUserRoleAssignmentRepository, UserRoleAssignmentRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<ILocalTokenService, LocalTokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // Sub-11 Phase 01 — Microsoft Graph user-create + CCE-side persist.
        // Factory is singleton (ClientSecretCredential is thread-safe and reusable);
        // service is scoped because it consumes the scoped CceDbContext.
        services.Configure<EntraIdOptions>(configuration.GetSection(EntraIdOptions.SectionName));
        services.AddSingleton<EntraIdGraphClientFactory>();
        services.AddScoped<EntraIdRegistrationService>();
        services.AddScoped<EntraIdUserSyncService>();

        // Sub-11d — outbound email transport. SMTP-backed when
        // Email:Provider=smtp; gateway-backed when Email:Provider=gateway;
        // otherwise NullEmailSender (logs + discards).
        // Singleton because all impls are stateless + thread-safe.
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddExternalApiClient<ICommunicationGatewayClient>("CommunicationGateway");
        services.AddExternalApiClient<CCE.Integration.AdminAuth.IAdminAuthGatewayClient>("AdminAuthGateway");
        services.AddSingleton<IEmailSender>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailOptions>>();
            var provider = (opts.Value.Provider ?? "null").ToLowerInvariant();
            return provider switch
            {
                "smtp"    => ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp),
                "gateway" => ActivatorUtilities.CreateInstance<global::CCE.Infrastructure.Communication.GatewayEmailSender>(sp),
                _         => ActivatorUtilities.CreateInstance<NullEmailSender>(sp),
            };
        });
        services.AddScoped<IStateRepAssignmentRepository, StateRepAssignmentRepository>();
        services.AddScoped<IExpertWorkflowRepository, ExpertWorkflowRepository>();
        services.AddScoped<IExpertRequestSubmissionRepository, ExpertRequestSubmissionRepository>();

        // US014 — KAPSARC Circular Carbon Economy classification-verification service.
        // Refit client + domain-friendly adapter (mirrors the email/SMS gateway pattern).
        services.AddExternalApiClient<CCE.Integration.Kapsarc.IKapsarcGatewayClient>("KapsarcGateway");
        services.AddScoped<CCE.Application.Kapsarc.IKapsarcClient, CCE.Infrastructure.Kapsarc.GatewayKapsarcClient>();

        // File storage — S3-backed (Supabase / MinIO / R2). Both asset and media slots
        // use the same singleton S3 client. LocalFileStorage is no longer registered.
        services.AddSingleton<IFileStorage, S3FileStorage>();
        services.AddKeyedSingleton<IFileStorage>("media", (sp, _) =>
            sp.GetRequiredService<IFileStorage>());
        services.AddSingleton<IFileStorageFactory, FileStorageFactory>();

        // Media upload options (bound from "Media" section in appsettings)
        services.Configure<MediaUploadOptions>(configuration.GetSection(MediaUploadOptions.SectionName));
        services.AddTransient<IClamAvScanner, ClamAvScanner>();
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizerWrapper>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        // ResourceCategory uses IRepository<ResourceCategory, Guid> (registered below)
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<INewsRepository, NewsRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IHomepageSectionRepository, HomepageSectionRepository>();
        services.AddScoped<ICountryContentRequestRepository, CountryContentRequestRepository>();
        services.AddScoped<IResourceViewCountRepository, ResourceViewCountRepository>();
        services.AddScoped<ICountryAdminService, CountryAdminService>();
        services.AddScoped<ICountryProfileService, CountryProfileService>();
        // Topic uses IRepository<Topic, Guid> (registered below)
        services.AddScoped<ICommunityModerationService, CommunityModerationService>();
        services.AddScoped<ICommunityWriteService, CommunityWriteService>();
        services.AddScoped<ICommunityVoteRepository, CommunityVoteRepository>();
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ICommunityRepository, CommunityRepository>();
        services.AddScoped<ICommunityAccessGuard, CommunityAccessGuard>();
        services.AddScoped<IReplyRepository, ReplyRepository>();
        services.AddScoped<IPollRepository, PollRepository>();
        services.AddScoped<IHomepageSettingsRepository, HomepageSettingsRepository>();
        services.AddScoped<IAboutSettingsRepository, AboutSettingsRepository>();
        services.AddScoped<IPoliciesSettingsRepository, PoliciesSettingsRepository>();

        services.AddScoped<IMediaFileRepository, MediaFileRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
        services.AddScoped<IUserNotificationSettingsRepository, UserNotificationSettingsRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<ICommunityReadService, CommunityReadService>();

        // Verification (OTP)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
        services.AddScoped<IUserVerificationRepository, UserVerificationRepository>();
        services.AddSingleton<IOtpCodeGenerator, OtpCodeGenerator>();

        // Notification gateway
        services.AddScoped<INotificationGateway, NotificationGateway>();
        services.AddScoped<INotificationMessageDispatcher, InProcessNotificationMessageDispatcher>();
        services.AddScoped<INotificationTemplateRenderer, NotificationTemplateRenderer>();
        services.AddScoped<INotificationChannelHandler, EmailNotificationChannelSender>();
        services.AddScoped<INotificationChannelHandler, SmsNotificationChannelSender>();
        services.AddScoped<INotificationChannelHandler, InAppNotificationChannelSender>();
        services.AddScoped<ISignalRNotificationPublisher, SignalRNotificationPublisher>();
        services.AddScoped<ICommunityRealtimePublisher, CommunityRealtimePublisher>();
        services.AddSingleton<CCE.Application.Common.Realtime.IRealtimePresenceTracker,
            CCE.Infrastructure.Notifications.RedisRealtimePresenceTracker>();
        services.AddScoped<IUserRegistrationsReportService, UserRegistrationsReportService>();
        services.AddScoped<IExpertReportService, ExpertReportService>();
        services.AddScoped<ISatisfactionSurveyReportService, SatisfactionSurveyReportService>();
        services.AddScoped<ICommunityPostReportService, CommunityPostReportService>();
        services.AddScoped<INewsReportService, NewsReportService>();
        services.AddScoped<IEventReportService, EventReportService>();
        services.AddScoped<IResourceReportService, ResourceReportService>();
        services.AddScoped<ICountryProfilesReportService, CountryProfilesReportService>();

        // Generic repository for aggregate roots
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        // Surveys
        services.AddScoped<IServiceRatingService, ServiceRatingService>();

        // Evaluation
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();

        // Smart assistant — factory routes to stub or Anthropic based on
        // Assistant:Provider config + ANTHROPIC_API_KEY env-var (Sub-10a).
        services.AddScoped<ICitationSearch, CitationSearch>();
        services.AddCceAssistantClient(configuration);

        // Interactive City
        services.AddScoped<ICityScenarioService, CityScenarioService>();

        // Messaging (MassTransit + EF outbox) — transport selected by Messaging:Transport in appsettings.
        // InMemory by default (no broker); set to RabbitMQ in production. Consumers run only where
        // registerConsumers=true (CCE.Worker); APIs/Seeder publish-only via the outbox.
        services.AddCceMessaging(configuration, registerConsumers);

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

        // Output-cache region invalidator (used by the cache-management endpoints and the
        // CacheInvalidationBehavior). Singleton — depends only on the singleton multiplexer.
        services.AddSingleton<CCE.Application.Common.Caching.IOutputCacheInvalidator,
            CCE.Infrastructure.Caching.RedisOutputCacheInvalidator>();

        // Raw Redis key inspector (used by the admin diagnostics endpoints).
        services.AddSingleton<CCE.Application.Common.Caching.IRedisKeyInspector,
            CCE.Infrastructure.Caching.RedisKeyInspector>();

        // Redis feed / hot-counter / leaderboard store (Spring 9).
        services.AddScoped<CCE.Application.Community.IRedisFeedStore,
            CCE.Infrastructure.Community.RedisFeedStore>();

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
