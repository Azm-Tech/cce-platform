using CCE.Application.Common.Interfaces;
using CCE.Domain.Common;
using CCE.Domain.Surveys;
using MediatR;

namespace CCE.Application.Surveys.Commands.SubmitServiceRating;

public sealed class SubmitServiceRatingCommandHandler
    : IRequestHandler<SubmitServiceRatingCommand, System.Guid>
{
    private readonly IServiceRatingService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public SubmitServiceRatingCommandHandler(
        IServiceRatingService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<System.Guid> Handle(
        SubmitServiceRatingCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var rating = ServiceRating.Submit(
            userId,
            request.Rating,
            request.CommentAr,
            request.CommentEn,
            request.Page,
            request.Locale,
            _clock);

        await _service.SaveAsync(rating, cancellationToken).ConfigureAwait(false);

        return rating.Id;
    }
}
