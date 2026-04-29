namespace CCE.Application.Community.Public.Dtos;

public sealed record PublicPostReplyDto(
    System.Guid Id,
    System.Guid PostId,
    System.Guid AuthorId,
    string Content,
    string Locale,
    System.Guid? ParentReplyId,
    bool IsByExpert,
    System.DateTimeOffset CreatedOn);
