using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.FollowPost;

public sealed class FollowPostCommandHandler : IRequestHandler<FollowPostCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public FollowPostCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(FollowPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot follow a post without a user identity.");

        // Idempotent: if already following, skip creation
        var existing = await _service.FindPostFollowAsync(request.PostId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return Unit.Value;

        var follow = PostFollow.Follow(request.PostId, userId, _clock);
        await _service.SaveFollowAsync(follow, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
