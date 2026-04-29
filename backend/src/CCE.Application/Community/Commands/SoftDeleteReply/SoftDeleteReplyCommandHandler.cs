using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SoftDeleteReply;

public sealed class SoftDeleteReplyCommandHandler : IRequestHandler<SoftDeleteReplyCommand, Unit>
{
    private readonly ICommunityModerationService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public SoftDeleteReplyCommandHandler(
        ICommunityModerationService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(SoftDeleteReplyCommand request, CancellationToken cancellationToken)
    {
        var reply = await _service.FindReplyAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (reply is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"PostReply {request.Id} not found.");
        }

        var moderatorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot moderate a reply from a request without a user identity.");

        reply.SoftDelete(moderatorId, _clock);
        await _service.UpdateReplyAsync(reply, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
