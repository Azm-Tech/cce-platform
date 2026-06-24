using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Surveys;
using MediatR;

namespace CCE.Application.Surveys.Commands.SubmitServiceRating;

public sealed class SubmitServiceRatingCommandHandler
    : IRequestHandler<SubmitServiceRatingCommand, Response<Guid>>
{
    private readonly IServiceRatingService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public SubmitServiceRatingCommandHandler(
        IServiceRatingService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory msg)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<Guid>> Handle(
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

        return _msg.Ok(rating.Id, MessageKeys.General.SUCCESS_CREATED);
    }
}
