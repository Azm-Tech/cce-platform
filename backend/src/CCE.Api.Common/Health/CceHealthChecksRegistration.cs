using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Health;

public static class CceHealthChecksRegistration
{
    public static IServiceCollection AddCceHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var sql = configuration["Infrastructure:SqlConnectionString"]
            ?? throw new InvalidOperationException("Infrastructure:SqlConnectionString missing.");
        var redis = configuration["Infrastructure:RedisConnectionString"]
            ?? throw new InvalidOperationException("Infrastructure:RedisConnectionString missing.");

        services.AddHealthChecks()
            .AddSqlServer(sql, name: "sqlserver", tags: ["ready"])
            .AddRedis(redis, name: "redis", tags: ["ready"]);

        return services;
    }
}
