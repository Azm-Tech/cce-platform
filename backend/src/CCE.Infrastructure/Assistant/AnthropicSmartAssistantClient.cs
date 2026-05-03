using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using CCE.Application.Assistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using SseEvent = CCE.Application.Assistant.SseEvent;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Production Smart Assistant client backed by Anthropic.SDK 5.0.0.
/// Streams Claude's `text_delta` events as TextEvents, then queries
/// CitationSearch for RAG-lite citations and yields CitationEvents,
/// then DoneEvent. Stream-open failure → ErrorEvent("server"); mid-
/// stream exception → partial text + ErrorEvent.
/// </summary>
// CA1031: catch (Exception) is the documented pattern for streaming I/O
// boundaries. The catch sites convert all SDK / network failures into
// SseEvent.Error events the frontend can surface; rethrowing would leak
// provider-internal types into the assistant's UX.
[SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Streaming boundary; converts to SseEvent.Error.")]
public sealed class AnthropicSmartAssistantClient : ISmartAssistantClient
{
    private static readonly Counter StreamsCounter = Metrics.CreateCounter(
        "cce_assistant_streams_runtime_total",
        "Anthropic stream invocations.",
        new CounterConfiguration { LabelNames = new[] { "provider" } });

    private static readonly Counter CitationsCounter = Metrics.CreateCounter(
        "cce_assistant_citations_runtime_total",
        "Citations emitted by the assistant.",
        new CounterConfiguration { LabelNames = new[] { "kind" } });

    private readonly IAnthropicStreamProvider _streamProvider;
    private readonly AnthropicOptions _options;
    private readonly ICitationSearch _citationSearch;
    private readonly ILogger<AnthropicSmartAssistantClient> _logger;

    public AnthropicSmartAssistantClient(
        IAnthropicStreamProvider streamProvider,
        IOptions<AnthropicOptions> options,
        ICitationSearch citationSearch,
        ILogger<AnthropicSmartAssistantClient> logger)
    {
        _streamProvider = streamProvider;
        _options = options.Value;
        _citationSearch = citationSearch;
        _logger = logger;
    }

    public async IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string locale,
        [EnumeratorCancellation] CancellationToken ct)
    {
        StreamsCounter.WithLabels("anthropic").Inc();

        var lastUser = messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        var sdkMessages = messages.Select(m => new Message(
            m.Role == "assistant" ? RoleType.Assistant : RoleType.User,
            m.Content,
            cacheControl: null!)).ToList();

        var systemPromptText = locale == "ar"
            ? "أنت مساعد منصة المعرفة لـ CCE. أجب باللغة العربية. كن موجزاً (2-4 جمل). تتعلق المواضيع بالاقتصاد الكربوني الدائري."
            : "You are the CCE Knowledge Center assistant. Answer in English. Be concise (2-4 sentences). Topics relate to circular carbon economy.";

        var parameters = new MessageParameters
        {
            Messages = sdkMessages,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = (decimal)_options.Temperature,
            System = new List<SystemMessage> { new(systemPromptText, cacheControl: null!) },
            Stream = true,
        };

        var assistantText = new System.Text.StringBuilder();
        var streamFailed = false;

        await foreach (var (response, error) in WithErrorHandling(parameters, ct).ConfigureAwait(false))
        {
            if (error)
            {
                streamFailed = true;
                break;
            }
            var text = response?.Delta?.Text;
            if (!string.IsNullOrEmpty(text))
            {
                assistantText.Append(text);
                yield return new TextEvent(text);
            }
        }

        if (streamFailed)
        {
            yield return new ErrorEvent(new ErrorPayload("server"));
            yield break;
        }

        // RAG-lite citation attachment.
        IReadOnlyList<CitationDto> citations;
        try
        {
            citations = await _citationSearch.FindCitationsAsync(
                lastUser, assistantText.ToString(), locale, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Citation search failed; continuing without citations.");
            citations = Array.Empty<CitationDto>();
        }

        foreach (var c in citations)
        {
            CitationsCounter.WithLabels(c.Kind).Inc();
            yield return new CitationEvent(c);
        }

        yield return new DoneEvent();
    }

    /// <summary>
    /// Wraps the SDK stream so any exception (open or mid-flight) becomes
    /// a (null, true) sentinel; lets the outer iterator yield an
    /// ErrorEvent before terminating without losing partial content.
    /// </summary>
    private async IAsyncEnumerable<(MessageResponse? Response, bool Error)> WithErrorHandling(
        MessageParameters parameters,
        [EnumeratorCancellation] CancellationToken ct)
    {
        IAsyncEnumerable<MessageResponse>? source = null;
        Exception? openError = null;
        try
        {
            source = _streamProvider.StreamClaudeMessageAsync(parameters, ct);
        }
        catch (Exception ex)
        {
            openError = ex;
        }
        if (openError is not null)
        {
            _logger.LogError(openError, "Anthropic stream open failed.");
            yield return (null, true);
            yield break;
        }

        var iterator = source!.GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                bool moveOk;
                Exception? caught = null;
                try
                {
                    moveOk = await iterator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    moveOk = false;
                    caught = ex;
                }

                if (caught is not null)
                {
                    _logger.LogError(caught, "Anthropic stream error mid-flight.");
                    yield return (null, true);
                    yield break;
                }
                if (!moveOk) yield break;
                yield return (iterator.Current, false);
            }
        }
        finally
        {
            await iterator.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Abstraction over <see cref="AnthropicClient.Messages"/> so unit tests
/// can mock the streaming behaviour. Production implementation is a
/// thin wrapper.
/// </summary>
public interface IAnthropicStreamProvider
{
    IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(
        MessageParameters parameters,
        CancellationToken ct);
}

/// <summary>Thin wrapper delegating to the SDK's MessagesEndpoint.</summary>
public sealed class AnthropicStreamProvider : IAnthropicStreamProvider
{
    private readonly AnthropicClient _client;
    public AnthropicStreamProvider(AnthropicClient client) => _client = client;
    public IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(
        MessageParameters parameters, CancellationToken ct)
        => _client.Messages.StreamClaudeMessageAsync(parameters, ct);
}
