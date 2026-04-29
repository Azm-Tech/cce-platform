using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.UnfollowUser;

public sealed class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public UnfollowUserCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        var followerId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot unfollow a user without a user identity.");

        await _service.RemoveUserFollowAsync(followerId, request.UserId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
