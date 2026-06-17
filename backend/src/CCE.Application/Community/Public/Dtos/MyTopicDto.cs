namespace CCE.Application.Community.Public.Dtos;

public sealed record MyTopicDto(
    System.Guid Id,
    string NameAr,
    string NameEn,
    bool IsWatchlisted,
    int PostsCount);
