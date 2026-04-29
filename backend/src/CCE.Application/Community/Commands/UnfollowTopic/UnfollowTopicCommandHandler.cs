using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.UnfollowTopic;

public sealed class UnfollowTopicCommandHandler : IRequestHandler<UnfollowTopicCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;

    public UnfollowTopicCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UnfollowTopicCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot unfollow a topic without a user identity.");

        // Idempotent: returns false when row doesn't exist — still 204
        await _service.RemoveTopicFollowAsync(request.TopicId, userId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
