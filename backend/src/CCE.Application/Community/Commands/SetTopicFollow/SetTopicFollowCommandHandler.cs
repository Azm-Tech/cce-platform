using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.SetTopicFollow;

public sealed class SetTopicFollowCommandHandler
    : IRequestHandler<SetTopicFollowCommand, Response<VoidData>>
{
    private readonly ICommunityWriteService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public SetTopicFollowCommandHandler(
        ICommunityWriteService service, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(SetTopicFollowCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        if (request.Status == FollowStatus.Followed)
        {
            var exists = await _db.Topics
                .AnyAsyncEither(t => t.Id == request.TopicId, cancellationToken).ConfigureAwait(false);
            if (!exists) return _msg.NotFound<VoidData>(MessageKeys.Community.TOPIC_NOT_FOUND);

            // Idempotent: only create when not already following
            var existing = await _service.FindTopicFollowAsync(request.TopicId, userId.Value, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                await _service.SaveFollowAsync(TopicFollow.Follow(request.TopicId, userId.Value, _clock), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Idempotent: no-ops when row is absent
            await _service.RemoveTopicFollowAsync(request.TopicId, userId.Value, cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(MessageKeys.General.SUCCESS_OPERATION);
    }
}
