using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record MyDraftDto(
    System.Guid Id,
    System.Guid TopicId,
    PostType Type,
    string? Title,
    string? Content,
    string Locale,
    System.DateTimeOffset CreatedOn,
    System.DateTimeOffset? LastModifiedOn);
