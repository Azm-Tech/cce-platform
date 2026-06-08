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

        var checks = services.AddHealthChecks()
            .AddSqlServer(sql, name: "sqlserver", tags: ["ready"])
            .AddRedis(redis, name: "redis", tags: ["ready"]);

        // RabbitMQ readiness — only when the bus uses it. A lightweight TCP probe (see
        // RabbitMqTcpHealthCheck) surfaces a broker outage on /health/ready without masking it.
        var transport = configuration["Messaging:Transport"];
        if (string.Equals(transport, "RabbitMQ", StringComparison.OrdinalIgnoreCase))
        {
            var (host, port) = ParseHostPort(configuration["Messaging:RabbitMqHost"]);
            checks.AddCheck("rabbitmq", new RabbitMqTcpHealthCheck(host, port), tags: ["ready"]);
        }

        return services;
    }

    private static (string Host, int Port) ParseHostPort(string? rabbitMqHost)
    {
        const int defaultPort = 5672;
        if (string.IsNullOrWhiteSpace(rabbitMqHost))
            return ("localhost", defaultPort);

        if (rabbitMqHost.Contains("://", StringComparison.Ordinal)
            && Uri.TryCreate(rabbitMqHost, UriKind.Absolute, out var uri))
            return (uri.Host, uri.Port > 0 ? uri.Port : defaultPort);

        var parts = rabbitMqHost.Split(':');
        return parts.Length == 2 && int.TryParse(parts[1], out var p)
            ? (parts[0], p)
            : (rabbitMqHost, defaultPort);
    }
}
