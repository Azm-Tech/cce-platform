namespace CCE.Application.Community.Public.Dtos;

/// <summary>Author summary embedded in <see cref="PostDetailDto"/>.</summary>
public sealed record PostAuthorDto(
    System.Guid Id,
    string Name,
    string? AvatarUrl,
    bool IsExpert,
    int PostsCount,
    int FollowerCount);
