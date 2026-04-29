using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.FollowUser;

public sealed class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public FollowUserCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(FollowUserCommand request, CancellationToken cancellationToken)
    {
        var followerId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot follow a user without a user identity.");

        // Idempotent: if already following, skip creation
        var existing = await _service.FindUserFollowAsync(followerId, request.UserId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return Unit.Value;

        var follow = UserFollow.Follow(followerId, request.UserId, _clock);
        await _service.SaveFollowAsync(follow, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
