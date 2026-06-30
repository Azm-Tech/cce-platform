using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using CCE.Application.Community.Moderation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Moderation;

public sealed class OllamaModerationProvider : IAiModerationProvider
{
    private readonly HttpClient _http;
    private readonly ModerationOptions _opts;
    private readonly ILogger<OllamaModerationProvider> _logger;

    public OllamaModerationProvider(
        HttpClient http,
        IOptions<ModerationOptions> opts,
        ILogger<OllamaModerationProvider> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
        _http.BaseAddress = new System.Uri(_opts.Ollama.BaseUrl.TrimEnd('/') + "/");
    }

    public string ProviderName => "ollama";

    public async Task<ModerationScore> ModerateAsync(string content, CancellationToken ct)
    {
        var prompt = BuildPrompt(content);
        var body = new
        {
            model  = _opts.Ollama.Model,
            stream = false,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        try
        {
            using var response = await _http
                .PostAsJsonAsync("api/chat", body, ct)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return ParseScore(text);
        }
        catch (System.Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Ollama moderation call failed; defaulting to Flagged");
            return ParseFailure(ex.Message);
        }
    }

    internal static string BuildPrompt(string content)
        => $$"""
            You are a strict content moderator for an online knowledge community.
            Respond with ONLY a JSON object and nothing else.
            Schema: {"safe":<true|false>,"confidence":<0.0-1.0>,"category":"<safe|spam|hate|explicit|harassment>","reason":"<short>"}

            Category definitions:
            - spam: advertising/promotion, scams, "buy now" pitches, prize/lottery/giveaway bait, get-rich-quick, repeated keywords, or content that is mostly links.
            - hate: attacks or dehumanizes people based on a protected trait (race, religion, nationality, gender, etc.).
            - explicit: sexual or pornographic content.
            - harassment: targeted insults, threats, or bullying of a specific person.
            - safe: none of the above.

            Rules:
            - If the content shows ANY clear sign of a violation, set safe=false and choose that category. Do NOT default to safe when unsure — lower the confidence instead.
            - confidence reflects how certain you are of the classification.

            Examples:
            Content: Buy cheap meds now!!! Click here to win a huge prize, limited time offer!
            {"safe":false,"confidence":0.96,"category":"spam","reason":"promotional scam with prize bait"}
            Content: Urban biodiversity supports sustainable cities and improves air quality for residents.
            {"safe":true,"confidence":0.97,"category":"safe","reason":""}

            Classify this content:
            {{content}}
            """;

    internal static ModerationScore ParseScore(string raw)
    {
        var json = StripFences(raw.Trim());
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var safe       = root.GetProperty("safe").GetBoolean();
            var confidence = (float)(root.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5);
            var category   = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "safe" : "safe";
            var reason     = root.TryGetProperty("reason", out var r) ? r.GetString() : null;
            return new ModerationScore(safe, confidence, category, reason);
        }
        catch (JsonException)
        {
            return ParseFailure(raw.Length > 200 ? raw[..200] : raw);
        }
        catch (InvalidOperationException)
        {
            return ParseFailure(raw.Length > 200 ? raw[..200] : raw);
        }
    }

    private static ModerationScore ParseFailure(string rawTruncated)
        => new(false, 0f, "parse-error", rawTruncated);

    private static string StripFences(string text)
    {
        if (text.StartsWith("```", System.StringComparison.Ordinal))
        {
            var start = text.IndexOf('\n', System.StringComparison.Ordinal) + 1;
            var end   = text.LastIndexOf("```", System.StringComparison.Ordinal);
            if (start > 0 && end > start)
                return text[start..end].Trim();
        }
        return text;
    }
}
