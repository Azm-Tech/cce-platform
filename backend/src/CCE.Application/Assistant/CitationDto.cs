namespace CCE.Application.Assistant;

public sealed record CitationDto(
    string Id,
    string Kind,
    string Title,
    string Href,
    string? SourceText);
