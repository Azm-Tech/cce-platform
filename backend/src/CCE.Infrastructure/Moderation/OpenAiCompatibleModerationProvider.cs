using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CCE.Application.Community.Moderation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Moderation;

/// <summary>
/// Moderation provider for any OpenAI-compatible chat-completions API
/// (Groq, OpenRouter, self-hosted vLLM, etc.).
/// </summary>
public sealed class OpenAiCompatibleModerationProvider : IAiModerationProvider
{
    private readonly HttpClient _http;
    private readonly ModerationOptions _opts;
    private readonly ILogger<OpenAiCompatibleModerationProvider> _logger;

    public OpenAiCompatibleModerationProvider(
        HttpClient http,
        IOptions<ModerationOptions> opts,
        ILogger<OpenAiCompatibleModerationProvider> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;

        _http.BaseAddress = new System.Uri(_opts.OpenAiCompatible.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(_opts.OpenAiCompatible.ApiKey))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _opts.OpenAiCompatible.ApiKey);
    }

    public string ProviderName => "openai-compatible";

    public async Task<ModerationScore> ModerateAsync(string content, CancellationToken ct)
    {
        var body = new
        {
            model       = _opts.OpenAiCompatible.Model,
            max_tokens  = 150,
            temperature = 0,
            messages    = new[]
            {
                new { role = "system", content = "You are a content moderator. Reply ONLY with valid JSON." },
                new { role = "user",   content = OllamaModerationProvider.BuildPrompt(content) }
            }
        };

        try
        {
            using var response = await _http
                .PostAsJsonAsync("v1/chat/completions", body, ct)
                .ConfigureAwait(false);

            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("AI provider rate-limited (429); flagging content for human review");
                return new ModerationScore(false, 0f, "rate-limited", "provider returned 429");
            }

            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(raw);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return OllamaModerationProvider.ParseScore(text);
        }
        catch (System.Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI-compatible moderation call failed; defaulting to Flagged");
            return new ModerationScore(false, 0f, "parse-error", ex.Message.Length > 200 ? ex.Message[..200] : ex.Message);
        }
    }
}
