using System.Text;
using System.Text.Json;
using CCE.Application.Assistant;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.External.Endpoints;

/// <summary>
/// Helper for writing IAsyncEnumerable&lt;SseEvent&gt; to an HTTP response
/// as text/event-stream. Each event is emitted as `data: {json}\n\n`.
/// </summary>
public static class SseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Enums-as-strings is wired globally by ConfigureHttpJsonOptions
        // (Sub-7 ship-readiness fix); we re-apply in case this writer is
        // ever used outside the standard pipeline.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task WriteAsync(
        HttpResponse response,
        IAsyncEnumerable<SseEvent> events,
        CancellationToken ct)
    {
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no"; // disable proxy buffering

        await foreach (var ev in events.WithCancellation(ct))
        {
            var json = JsonSerializer.Serialize<SseEvent>(ev, JsonOptions);
            var frame = Encoding.UTF8.GetBytes($"data: {json}\n\n");
            await response.Body.WriteAsync(frame, ct).ConfigureAwait(false);
            await response.Body.FlushAsync(ct).ConfigureAwait(false);
        }
    }
}
