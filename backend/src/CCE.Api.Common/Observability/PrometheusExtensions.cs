using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Prometheus;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Prometheus middleware + /metrics endpoint. Custom assistant counters
/// (cce_assistant_streams_total{provider} and
/// cce_assistant_citations_total{kind}) are exposed for the LLM
/// observability story.
/// </summary>
public static class PrometheusExtensions
{
    /// <summary>Stream calls, labeled by provider (stub | anthropic).</summary>
    public static readonly Counter AssistantStreamsTotal = Metrics
        .CreateCounter(
            "cce_assistant_streams_total",
            "Total assistant stream requests, labeled by provider.",
            new CounterConfiguration { LabelNames = new[] { "provider" } });

    /// <summary>Citations emitted, labeled by kind (resource | map-node).</summary>
    public static readonly Counter AssistantCitationsTotal = Metrics
        .CreateCounter(
            "cce_assistant_citations_total",
            "Total citations emitted by the assistant, labeled by kind.",
            new CounterConfiguration { LabelNames = new[] { "kind" } });

    /// <summary>Community search requests, labeled by sort (relevance|Hot|Newest|TopVoted|MostCommented).</summary>
    public static readonly Counter CommunitySearchHitsTotal = Metrics
        .CreateCounter(
            "community_search_hits",
            "Total community search requests served, labeled by effective sort order.",
            new CounterConfiguration { LabelNames = new[] { "sort" } });

    /// <summary>Community search end-to-end duration in milliseconds (Meilisearch + DB + hydration).</summary>
    public static readonly Histogram CommunitySearchDurationMs = Metrics
        .CreateHistogram(
            "community_search_duration_ms",
            "End-to-end duration of community search requests in milliseconds.",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(start: 10, factor: 2, count: 10),
            });

    public static WebApplication UseCcePrometheus(this WebApplication app)
    {
        app.UseHttpMetrics();
        app.MapMetrics("/metrics").AllowAnonymous();
        return app;
    }
}
