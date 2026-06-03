using CCE.Application.Common;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryEventRequest;

public sealed class SubmitCountryEventRequestCommandHandler
    : IRequestHandler<SubmitCountryEventRequestCommand, Response<System.Guid>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICountryScopeAccessor _scope;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;
    private readonly INotificationMessageDispatcher _dispatcher;

    public SubmitCountryEventRequestCommandHandler(
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ICountryScopeAccessor scope,
        ISystemClock clock,
        MessageFactory messages,
        INotificationMessageDispatcher dispatcher)
    {
        _db = db;
        _currentUser = currentUser;
        _scope = scope;
        _clock = clock;
        _messages = messages;
        _dispatcher = dispatcher;
    }

    public async Task<Response<System.Guid>> Handle(
        SubmitCountryEventRequestCommand request,
        CancellationToken cancellationToken)
    {
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);
        if (authorizedIds is not null && !authorizedIds.Contains(request.CountryId))
            return _messages.CountryScopeForbidden<System.Guid>();

        var topics = await _db.Topics
            .Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        if (topics.Count == 0)
            return _messages.TopicNotFound<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot submit without a user identity.");

        var contentRequest = CountryContentRequest.SubmitEvent(
            request.CountryId, userId,
            request.TitleAr, request.TitleEn,
            request.DescriptionAr, request.DescriptionEn,
            request.TopicId,
            request.StartsOn, request.EndsOn,
            request.LocationAr, request.LocationEn, request.OnlineMeetingUrl,
            _clock);

        _db.Add(contentRequest);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: "COUNTRY_CONTENT_SUBMITTED",
            RecipientUserId: null,
            EventType: NotificationEventType.CountryContentSubmitted,
            Channels: [NotificationChannel.InApp, NotificationChannel.Email],
            MetaData: new Dictionary<string, string>
            {
                ["RequestId"] = contentRequest.Id.ToString(),
                ["Kind"] = contentRequest.Kind.ToString(),
            }),
            cancellationToken).ConfigureAwait(false);

        return _messages.Ok(contentRequest.Id, ApplicationErrors.Content.COUNTRY_CONTENT_REQUEST_SUBMITTED);
    }
}
