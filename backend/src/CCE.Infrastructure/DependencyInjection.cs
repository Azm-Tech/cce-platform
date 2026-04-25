using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // EF Core — SQL Server with snake_case naming
        services.AddDbContext<CceDbContext>((sp, opts) =>
        {
            var infraOpts = sp.GetRequiredService<IOptions<CceInfrastructureOptions>>().Value;
            opts.UseSqlServer(infraOpts.SqlConnectionString);
            opts.UseSnakeCaseNamingConvention();
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
}
