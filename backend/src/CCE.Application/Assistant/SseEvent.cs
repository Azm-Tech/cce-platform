using System.Text.Json.Serialization;

namespace CCE.Application.Assistant;

/// <summary>
/// Discriminated union of SSE events emitted by the assistant stream.
/// JSON discriminator is `type` to match the frontend SseEvent shape.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextEvent), typeDiscriminator: "text")]
[JsonDerivedType(typeof(CitationEvent), typeDiscriminator: "citation")]
[JsonDerivedType(typeof(DoneEvent), typeDiscriminator: "done")]
[JsonDerivedType(typeof(ErrorEvent), typeDiscriminator: "error")]
public abstract record SseEvent;

public sealed record TextEvent(string Content) : SseEvent;

public sealed record CitationEvent(CitationDto Citation) : SseEvent;

public sealed record DoneEvent : SseEvent;

public sealed record ErrorEvent(ErrorPayload Error) : SseEvent;

public sealed record ErrorPayload(string Kind);
