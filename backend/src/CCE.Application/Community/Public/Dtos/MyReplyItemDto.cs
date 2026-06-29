namespace CCE.Application.Community.Public.Dtos;

public sealed record MyReplyItemDto(
    System.Guid           ReplyId,
    System.Guid           PostId,
    string                PostTitle,
    System.Guid           AuthorId,
    string                AuthorName,
    string?               Content,
    System.DateTimeOffset CreatedOn,
    int                   UpvoteCount,
    int                   DownvoteCount);
