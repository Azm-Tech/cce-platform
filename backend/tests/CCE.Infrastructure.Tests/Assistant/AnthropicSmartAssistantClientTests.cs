using Anthropic.SDK.Messaging;
using CCE.Application.Assistant;
using CCE.Infrastructure.Assistant;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SseEvent = CCE.Application.Assistant.SseEvent;

namespace CCE.Infrastructure.Tests.Assistant;

public class AnthropicSmartAssistantClientTests
{
    [Fact]
    public async Task Yields_text_chunks_then_citations_then_done()
    {
        var provider = new FakeStreamProvider(
            FakeResponse("Hello "),
            FakeResponse("world"));
        var citationSearch = new FakeCitationSearch(
            new CitationDto("r1", "resource", "Some Resource", "/x", null));
        var sut = Build(provider, citationSearch);

        var events = await Drain(sut);

        events.OfType<TextEvent>().Select(t => t.Content).Should().Equal("Hello ", "world");
        events.OfType<CitationEvent>().Should().ContainSingle();
        events.Last().Should().BeOfType<DoneEvent>();
    }

    [Fact]
    public async Task Stream_open_failure_yields_ErrorEvent()
    {
        var provider = new FakeStreamProvider(throwOnOpen: true);
        var sut = Build(provider, new FakeCitationSearch());

        var events = await Drain(sut);

        events.Should().ContainSingle().Which.Should().BeOfType<ErrorEvent>();
    }

    [Fact]
    public async Task Mid_stream_exception_yields_partial_text_then_ErrorEvent()
    {
        var provider = new FakeStreamProvider(
            new[] { FakeResponse("Partial") },
            throwAfterIndex: 1);
        var sut = Build(provider, new FakeCitationSearch());

        var events = await Drain(sut);

        events.OfType<TextEvent>().Single().Content.Should().Be("Partial");
        events.Last().Should().BeOfType<ErrorEvent>();
    }

    [Fact]
    public async Task Citation_search_failure_continues_without_citations()
    {
        var provider = new FakeStreamProvider(FakeResponse("ok"));
        var citationSearch = new FakeCitationSearch(throwOnSearch: true);
        var sut = Build(provider, citationSearch);

        var events = await Drain(sut);

        events.OfType<CitationEvent>().Should().BeEmpty();
        events.Last().Should().BeOfType<DoneEvent>();
    }

    private static AnthropicSmartAssistantClient Build(
        IAnthropicStreamProvider provider, ICitationSearch citationSearch)
        => new(
            provider,
            Options.Create(new AnthropicOptions()),
            citationSearch,
            NullLogger<AnthropicSmartAssistantClient>.Instance);

    private static async Task<List<SseEvent>> Drain(AnthropicSmartAssistantClient sut)
    {
        var list = new List<SseEvent>();
        await foreach (var e in sut.StreamAsync(
            new[] { new ChatMessage("user", "hi") }, "en", default))
        {
            list.Add(e);
        }
        return list;
    }

    private static MessageResponse FakeResponse(string text) => new()
    {
        Delta = new Delta { Text = text, Type = "text_delta" },
    };

    private sealed class FakeStreamProvider : IAnthropicStreamProvider
    {
        private readonly MessageResponse[] _responses;
        private readonly bool _throwOnOpen;
        private readonly int? _throwAfterIndex;

        public FakeStreamProvider(params MessageResponse[] responses)
        {
            _responses = responses;
            _throwOnOpen = false;
            _throwAfterIndex = null;
        }

        public FakeStreamProvider(bool throwOnOpen)
        {
            _responses = Array.Empty<MessageResponse>();
            _throwOnOpen = throwOnOpen;
            _throwAfterIndex = null;
        }

        public FakeStreamProvider(MessageResponse[] responses, int throwAfterIndex)
        {
            _responses = responses;
            _throwOnOpen = false;
            _throwAfterIndex = throwAfterIndex;
        }

        public IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(
            MessageParameters parameters, CancellationToken ct)
        {
            if (_throwOnOpen) throw new InvalidOperationException("open failed");
            return Iterate(ct);
        }

        private async IAsyncEnumerable<MessageResponse> Iterate(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            for (int i = 0; i < _responses.Length; i++)
            {
                if (_throwAfterIndex.HasValue && i == _throwAfterIndex.Value)
                {
                    throw new InvalidOperationException("mid-stream failure");
                }
                yield return _responses[i];
                await Task.Yield();
            }
            if (_throwAfterIndex.HasValue && _throwAfterIndex.Value >= _responses.Length)
            {
                throw new InvalidOperationException("post-stream failure");
            }
        }
    }

    private sealed class FakeCitationSearch : ICitationSearch
    {
        private readonly IReadOnlyList<CitationDto> _citations;
        private readonly bool _throwOnSearch;

        public FakeCitationSearch(params CitationDto[] citations)
        {
            _citations = citations;
            _throwOnSearch = false;
        }

        public FakeCitationSearch(bool throwOnSearch)
        {
            _citations = Array.Empty<CitationDto>();
            _throwOnSearch = throwOnSearch;
        }

        public Task<IReadOnlyList<CitationDto>> FindCitationsAsync(
            string userQuestion, string assistantReply, string locale, CancellationToken ct)
        {
            if (_throwOnSearch) throw new InvalidOperationException("citation search failed");
            return Task.FromResult(_citations);
        }
    }
}
