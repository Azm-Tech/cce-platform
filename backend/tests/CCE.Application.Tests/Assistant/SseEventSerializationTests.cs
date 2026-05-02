using System.Text.Json;
using CCE.Application.Assistant;
using FluentAssertions;
using Xunit;

namespace CCE.Application.Tests.Assistant;

public class SseEventSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void TextEvent_serializes_to_camelCase_with_type_discriminator()
    {
        SseEvent ev = new TextEvent("Hello");
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be("""{"type":"text","content":"Hello"}""");
    }

    [Fact]
    public void CitationEvent_serializes_with_nested_citation_payload()
    {
        SseEvent ev = new CitationEvent(new CitationDto(
            Id: "r1", Kind: "resource", Title: "T", Href: "/x", SourceText: null));
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be(
            """{"type":"citation","citation":{"id":"r1","kind":"resource","title":"T","href":"/x","sourceText":null}}""");
    }

    [Fact]
    public void DoneEvent_serializes_to_just_a_type_field()
    {
        SseEvent ev = new DoneEvent();
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be("""{"type":"done"}""");
    }

    [Fact]
    public void ErrorEvent_serializes_with_error_payload()
    {
        SseEvent ev = new ErrorEvent(new ErrorPayload("network"));
        var json = JsonSerializer.Serialize(ev, JsonOptions);
        json.Should().Be("""{"type":"error","error":{"kind":"network"}}""");
    }
}
