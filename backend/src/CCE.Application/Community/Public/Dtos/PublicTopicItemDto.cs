namespace CCE.Application.Community.Public.Dtos;

public sealed record PublicTopicItemDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    int PostsCount,
    bool IsWatchlisted);
