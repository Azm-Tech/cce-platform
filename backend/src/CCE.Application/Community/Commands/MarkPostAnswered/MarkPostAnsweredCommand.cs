using MediatR;

namespace CCE.Application.Community.Commands.MarkPostAnswered;

public sealed record MarkPostAnsweredCommand(
    Guid PostId,
    Guid ReplyId) : IRequest<Unit>;
