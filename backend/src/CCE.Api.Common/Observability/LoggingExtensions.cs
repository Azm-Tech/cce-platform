using Microsoft.Extensions.Hosting;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Host-level Serilog wiring for both APIs. Phase 02 fills in the
/// console + rolling-file + Sentry sinks plus correlation-id /
/// locale / user-id enrichers. Phase 00 is a no-op pass-through so
/// Dockerfiles can compile against the stable surface.
/// </summary>
public static class LoggingExtensions
{
    public static IHostBuilder UseCceSerilog(this IHostBuilder builder)
    {
        // Phase 02 Task 2.1: wire UseSerilog with sinks + enrichers.
        return builder;
    }
}
