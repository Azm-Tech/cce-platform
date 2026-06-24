using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Community.Commands.EditReply;

public sealed record EditReplyCommand(
    Guid ReplyId,
    string Content) : IRequest<Response<VoidData>>;
