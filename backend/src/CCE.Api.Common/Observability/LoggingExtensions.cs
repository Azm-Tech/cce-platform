using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sentry.Serilog;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Host-level Serilog wiring. Console JSON-compact for kubectl/docker logs,
/// rolling-file (daily) for on-host inspection, optional Sentry sink for
/// warning+ events when SENTRY_DSN is set. Correlation-id flows from the
/// existing CorrelationIdMiddleware via BeginScope + FromLogContext.
/// </summary>
public static class LoggingExtensions
{
    public static IHostBuilder UseCceSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((ctx, cfg) =>
        {
            var minLevel = ParseLevel(ctx.Configuration["Serilog:MinimumLevel"])
                ?? LogEventLevel.Information;
            var fileEnabled = ctx.Configuration.GetValue<bool>("Serilog:FileSink:Enabled");
            var filePath = ctx.Configuration["Serilog:FileSink:Path"] ?? "logs/cce-.log";
            var retainedDays = ctx.Configuration.GetValue<int?>("Serilog:FileSink:RetainedDays") ?? 7;
            var sentryDsn = ctx.Configuration["SENTRY_DSN"]
                         ?? Environment.GetEnvironmentVariable("SENTRY_DSN");

            cfg
                .MinimumLevel.Is(minLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app", ctx.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("env", ctx.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());

            if (fileEnabled)
            {
                cfg.WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedDays);
            }

            if (!string.IsNullOrWhiteSpace(sentryDsn))
            {
                cfg.WriteTo.Sentry(o =>
                {
                    o.Dsn = sentryDsn;
                    o.MinimumEventLevel = LogEventLevel.Warning;
                    o.MinimumBreadcrumbLevel = LogEventLevel.Information;
                });
            }
        });
    }

    private static LogEventLevel? ParseLevel(string? value)
        => Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var lvl) ? lvl : null;
}
