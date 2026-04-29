using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.UnfollowPost;

public sealed class UnfollowPostCommandHandler : IRequestHandler<UnfollowPostCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public UnfollowPostCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UnfollowPostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot unfollow a post without a user identity.");

        await _service.RemovePostFollowAsync(request.PostId, userId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
