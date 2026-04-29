using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.RatePost;

public sealed class RatePostCommandHandler : IRequestHandler<RatePostCommand, Unit>
{
    private readonly ICommunityWriteService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public RatePostCommandHandler(
        ICommunityWriteService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(RatePostCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot rate a post without a user identity.");

        var post = await _service.FindPostAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null)
        {
            throw new KeyNotFoundException($"Post {request.PostId} not found.");
        }

        var rating = PostRating.Rate(request.PostId, userId, request.Stars, _clock);
        await _service.SaveRatingAsync(rating, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
