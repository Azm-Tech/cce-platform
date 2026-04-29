using MediatR;

namespace CCE.Application.Community.Commands.RatePost;

public sealed record RatePostCommand(
    Guid PostId,
    int Stars) : IRequest<Unit>;
