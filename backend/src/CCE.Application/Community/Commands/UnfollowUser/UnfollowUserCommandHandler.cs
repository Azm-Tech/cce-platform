using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Commands.UnfollowUser;

public sealed class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public UnfollowUserCommandHandler(
        ICommunityWriteService service,
        ICceDbContext db,
        ICurrentUserAccessor currentUser)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        var followerId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot unfollow a user without a user identity.");

        var removed = await _service.RemoveUserFollowAsync(followerId, request.UserId, cancellationToken).ConfigureAwait(false);
        if (removed)
        {
            var follower = await _db.Users.FirstOrDefaultAsync(u => u.Id == followerId, cancellationToken).ConfigureAwait(false);
            var followed = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken).ConfigureAwait(false);
            follower?.DecrementFollowing();
            followed?.DecrementFollowers();
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
