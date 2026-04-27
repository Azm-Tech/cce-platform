using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
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
    }
}
