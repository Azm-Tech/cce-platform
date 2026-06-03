using CCE.Application.Common;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Errors;
using CCE.Application.Messages;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Notifications;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryNewsRequest;

public sealed class SubmitCountryNewsRequestCommandHandler
    : IRequestHandler<SubmitCountryNewsRequestCommand, Response<System.Guid>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICountryScopeAccessor _scope;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;
    private readonly INotificationMessageDispatcher _dispatcher;

    public SubmitCountryNewsRequestCommandHandler(
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
        SubmitCountryNewsRequestCommand request,
        CancellationToken cancellationToken)
    {
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);
        if (authorizedIds is not null && !authorizedIds.Contains(request.CountryId))
            return _messages.CountryScopeForbidden<System.Guid>();

        // Validate topic exists
        var topics = await _db.Topics
            .Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        if (topics.Count == 0)
            return _messages.TopicNotFound<System.Guid>();

        // Validate optional featured image
        if (request.FeaturedImageAssetId.HasValue)
        {
            var assets = await _db.AssetFiles
                .Where(a => a.Id == request.FeaturedImageAssetId.Value)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            var asset = assets.FirstOrDefault();
            if (asset is null)
                return _messages.AssetNotFound<System.Guid>();
            if (asset.VirusScanStatus != VirusScanStatus.Clean)
                return _messages.AssetNotClean<System.Guid>();
        }

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot submit without a user identity.");

        var contentRequest = CountryContentRequest.SubmitNews(
            request.CountryId, userId,
            request.TitleAr, request.TitleEn,
            request.ContentAr, request.ContentEn,
            request.TopicId, request.FeaturedImageAssetId,
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
