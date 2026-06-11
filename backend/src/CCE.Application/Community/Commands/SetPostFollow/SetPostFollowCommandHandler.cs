using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.SetPostFollow;

public sealed class SetPostFollowCommandHandler
    : IRequestHandler<SetPostFollowCommand, Response<VoidData>>
{
    private readonly ICommunityWriteService _service;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public SetPostFollowCommandHandler(
        ICommunityWriteService service, ICceDbContext db, ICurrentUserAccessor currentUser,
        ISystemClock clock, MessageFactory msg)
    {
        _service = service;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(SetPostFollowCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        if (request.Status == FollowStatus.Followed)
        {
            var exists = await _db.Posts
                .AnyAsyncEither(p => p.Id == request.PostId, cancellationToken).ConfigureAwait(false);
            if (!exists) return _msg.NotFound<VoidData>(ApplicationErrors.Community.POST_NOT_FOUND);

            // Idempotent: only create when not already following
            var existing = await _service.FindPostFollowAsync(request.PostId, userId.Value, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                await _service.SaveFollowAsync(PostFollow.Follow(request.PostId, userId.Value, _clock), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Idempotent: no-ops when row is absent
            await _service.RemovePostFollowAsync(request.PostId, userId.Value, cancellationToken).ConfigureAwait(false);
        }

        return _msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION);
    }
}
