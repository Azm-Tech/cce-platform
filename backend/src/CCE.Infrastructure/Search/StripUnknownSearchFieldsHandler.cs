namespace CCE.Infrastructure.Search;

// Strips fields that the SDK serializes by default but that the running Meilisearch version
// does not recognise. Only applied to POST /.../search requests; all other traffic is forwarded
// unchanged.
internal sealed class StripUnknownSearchFieldsHandler : System.Net.Http.DelegatingHandler
{
    private static readonly System.Collections.Generic.HashSet<string> FieldsToStrip =
        new(System.StringComparer.Ordinal) { "rankingScoreThreshold" };

    protected override async System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(
        System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken ct)
    {
        if (request.Content is not null &&
            request.Method == System.Net.Http.HttpMethod.Post &&
            request.RequestUri?.AbsolutePath.EndsWith("/search", System.StringComparison.OrdinalIgnoreCase) == true)
        {
            var json = await request.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (ContainsAny(json))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                using var ms  = new System.IO.MemoryStream();
                using var w   = new System.Text.Json.Utf8JsonWriter(ms);
                w.WriteStartObject();
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (!FieldsToStrip.Contains(prop.Name))
                        prop.WriteTo(w);
                }
                w.WriteEndObject();
                await w.FlushAsync(ct).ConfigureAwait(false);
                request.Content = new System.Net.Http.StringContent(
                    System.Text.Encoding.UTF8.GetString(ms.ToArray()),
                    System.Text.Encoding.UTF8, "application/json");
            }
        }
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }

    private static bool ContainsAny(string json)
    {
        foreach (var f in FieldsToStrip)
            if (json.Contains(f, System.StringComparison.Ordinal)) return true;
        return false;
    }
}
