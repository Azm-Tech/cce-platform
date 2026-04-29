using MediatR;

namespace CCE.Application.Community.Commands.CreateReply;

public sealed record CreateReplyCommand(
    Guid PostId,
    string Content,
    string Locale,
    Guid? ParentReplyId) : IRequest<Guid>;
