using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.FollowTopic;

public sealed class FollowTopicCommandHandler : IRequestHandler<FollowTopicCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public FollowTopicCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(FollowTopicCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot follow a topic without a user identity.");

        // Idempotent: if already following, skip creation
        var existing = await _service.FindTopicFollowAsync(request.TopicId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return Unit.Value;

        var follow = TopicFollow.Follow(request.TopicId, userId, _clock);
        await _service.SaveFollowAsync(follow, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
