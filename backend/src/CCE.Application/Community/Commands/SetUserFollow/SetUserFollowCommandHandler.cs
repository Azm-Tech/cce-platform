using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.Identity;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.SetUserFollow;

public sealed class SetUserFollowCommandHandler
    : IRequestHandler<SetUserFollowCommand, Response<VoidData>>
{
    private readonly ICommunityWriteService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;
    private readonly IUserRepository _userRepo;

    public SetUserFollowCommandHandler(
        ICommunityWriteService service, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg, IUserRepository userRepo)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
        _userRepo = userRepo;
    }

    public async Task<Response<VoidData>> Handle(SetUserFollowCommand request, CancellationToken cancellationToken)
    {
        var followerId = _currentUser.GetUserId();
        if (followerId is null || followerId == Guid.Empty) return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        if (request.Status == FollowStatus.Followed)
        {
            if (followerId.Value == request.UserId) return _msg.ValidationError<VoidData>(MessageKeys.Community.CANNOT_FOLLOW_SELF, new[] { _msg.Field("userId", MessageKeys.Community.CANNOT_FOLLOW_SELF) });

            var followed = await _userRepo.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
            if (followed is null) return _msg.NotFound<VoidData>(MessageKeys.Identity.USER_NOT_FOUND);

            // Idempotent: only create + bump counts when not already following
            var existing = await _service.FindUserFollowAsync(followerId.Value, request.UserId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                await _service.SaveFollowAsync(UserFollow.Follow(followerId.Value, request.UserId, _clock), cancellationToken).ConfigureAwait(false);

                var follower = await _userRepo.FindAsync(followerId.Value, cancellationToken).ConfigureAwait(false);
                follower?.IncrementFollowing();
                followed.IncrementFollowers();
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            var removed = await _service.RemoveUserFollowAsync(followerId.Value, request.UserId, cancellationToken).ConfigureAwait(false);
            if (removed)
            {
                var follower = await _userRepo.FindAsync(followerId.Value, cancellationToken).ConfigureAwait(false);
                var followed = await _userRepo.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
                follower?.DecrementFollowing();
                followed?.DecrementFollowers();
                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
