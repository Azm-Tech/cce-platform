using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Community.Commands.SoftDeletePost;

public sealed class SoftDeletePostCommandHandler : IRequestHandler<SoftDeletePostCommand, Unit>
{
    private readonly ICommunityModerationService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public SoftDeletePostCommandHandler(
        ICommunityModerationService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(SoftDeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _service.FindPostAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Post {request.Id} not found.");
        }

        var moderatorId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot moderate a post from a request without a user identity.");

        post.SoftDelete(moderatorId, _clock);
        await _service.UpdatePostAsync(post, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
