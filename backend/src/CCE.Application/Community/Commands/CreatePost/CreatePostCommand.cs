using MediatR;

namespace CCE.Application.Community.Commands.CreatePost;

public sealed record CreatePostCommand(
    Guid TopicId,
    string Content,
    string Locale,
    bool IsAnswerable) : IRequest<Guid>;
