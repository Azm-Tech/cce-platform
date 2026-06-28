namespace CCE.Application.Content.Public.Dtos;

public sealed record ShareLinkDto(
    string Link,
    string Title,
    string? ImageUrl);
