using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Community;
using CCE.Application.Content;
using CCE.Application.Country;
using CCE.Application.Identity;
using CCE.Application.Notifications;
using CCE.Application.Reports;
using CCE.Infrastructure.Community;
using CCE.Infrastructure.Content;
using CCE.Infrastructure.Sanitization;
using CCE.Infrastructure.Country;
using CCE.Infrastructure.Notifications;
using CCE.Infrastructure.Reports;
using CCE.Domain.Common;
using CCE.Infrastructure.Files;
using CCE.Infrastructure.Identity;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Interceptors;
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
        services.AddScoped<IStateRepAssignmentService, StateRepAssignmentService>();
        services.AddScoped<IExpertWorkflowService, ExpertWorkflowService>();

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
        services.AddScoped<ICountryAdminService, CountryAdminService>();
        services.AddScoped<ICountryProfileService, CountryProfileService>();
        services.AddScoped<ITopicService, TopicService>();
        services.AddScoped<ICommunityModerationService, CommunityModerationService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<IUserRegistrationsReportService, UserRegistrationsReportService>();
        services.AddScoped<IExpertReportService, ExpertReportService>();
        services.AddScoped<ISatisfactionSurveyReportService, SatisfactionSurveyReportService>();
        services.AddScoped<ICommunityPostReportService, CommunityPostReportService>();
        services.AddScoped<INewsReportService, NewsReportService>();
        services.AddScoped<IEventReportService, EventReportService>();
        services.AddScoped<IResourceReportService, ResourceReportService>();
        services.AddScoped<ICountryProfilesReportService, CountryProfilesReportService>();

        // Redis — singleton multiplexer
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            return ConnectionMultiplexer.Connect(infraOpts.RedisConnectionString);
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
