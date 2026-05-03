using Microsoft.AspNetCore.Builder;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Prometheus middleware + /metrics endpoint for both APIs. Phase 02
/// fills in UseHttpMetrics() + MapMetrics("/metrics") plus the two
/// custom counters (cce_assistant_streams_total{provider} and
/// cce_assistant_citations_total{kind}). Phase 00 is a no-op
/// pass-through so Dockerfiles can compile against the stable surface.
/// </summary>
public static class PrometheusExtensions
{
    public static WebApplication UseCcePrometheus(this WebApplication app)
    {
        // Phase 02 Task 2.2: wire UseHttpMetrics() + MapMetrics() + custom counters.
        return app;
    }
}
