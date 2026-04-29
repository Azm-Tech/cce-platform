namespace CCE.Application.Community.Public.Dtos;

public sealed record PublicPostDto(
    System.Guid Id,
    System.Guid TopicId,
    System.Guid AuthorId,
    string Content,
    string Locale,
    bool IsAnswerable,
    System.Guid? AnsweredReplyId,
    System.DateTimeOffset CreatedOn);
