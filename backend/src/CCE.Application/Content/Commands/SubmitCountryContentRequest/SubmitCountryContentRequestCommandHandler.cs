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

namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

public sealed class SubmitCountryContentRequestCommandHandler
    : IRequestHandler<SubmitCountryContentRequestCommand, Response<System.Guid>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICountryScopeAccessor _scope;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;
    private readonly INotificationMessageDispatcher _dispatcher;

    public SubmitCountryContentRequestCommandHandler(
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
        SubmitCountryContentRequestCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot submit without a user identity.");

        var countryId = await ResolveCountryIdAsync(request.CountryId, cancellationToken).ConfigureAwait(false);
        if (countryId is null)
            return _messages.CountryScopeForbidden<System.Guid>();

        CountryContentRequest contentRequest = request.Content switch
        {
            CreateResourceBody body => await SubmitResourceAsync(body, countryId.Value, userId, cancellationToken).ConfigureAwait(false),
            CreateNewsBody body => await SubmitNewsAsync(body, countryId.Value, userId, cancellationToken).ConfigureAwait(false),
            CreateEventBody body => await SubmitEventAsync(body, countryId.Value, userId, cancellationToken).ConfigureAwait(false),
            _ => throw new DomainException("Invalid content type.")
        };

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
                ["Type"] = contentRequest.Type.ToString(),
            }),
            cancellationToken).ConfigureAwait(false);

        return _messages.Ok(contentRequest.Id, ApplicationErrors.Content.COUNTRY_CONTENT_REQUEST_SUBMITTED);
    }

    private async Task<System.Guid?> ResolveCountryIdAsync(
        System.Guid? countryId,
        CancellationToken ct)
    {
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(ct).ConfigureAwait(false);

        if (countryId is null)
        {
            if (authorizedIds is null || authorizedIds.Count == 0)
                return null;
            return authorizedIds[0];
        }

        if (authorizedIds is not null && !authorizedIds.Contains(countryId.Value))
            return null;

        return countryId;
    }

    private async Task<CountryContentRequest> SubmitResourceAsync(
        CreateResourceBody body,
        System.Guid countryId,
        System.Guid userId,
        CancellationToken ct)
    {
        var assets = await _db.AssetFiles
            .Where(a => a.Id == body.AssetFileId)
            .ToListAsyncEither(ct).ConfigureAwait(false);
        var asset = assets.FirstOrDefault();
        if (asset is null)
            throw new DomainException("Asset not found.");
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            throw new DomainException("Asset is not clean.");

        return CountryContentRequest.SubmitResource(
            countryId, userId,
            body.TitleAr, body.TitleEn,
            body.DescriptionAr, body.DescriptionEn,
            body.ResourceType, body.AssetFileId,
            _clock);
    }

    private async Task<CountryContentRequest> SubmitNewsAsync(
        CreateNewsBody body,
        System.Guid countryId,
        System.Guid userId,
        CancellationToken ct)
    {
        var topics = await _db.Topics
            .Where(t => t.Id == body.TopicId)
            .ToListAsyncEither(ct).ConfigureAwait(false);
        if (topics.Count == 0)
            throw new DomainException("Topic not found.");

        if (body.FeaturedImageAssetId.HasValue)
        {
            var assets = await _db.AssetFiles
                .Where(a => a.Id == body.FeaturedImageAssetId.Value)
                .ToListAsyncEither(ct).ConfigureAwait(false);
            var asset = assets.FirstOrDefault();
            if (asset is null)
                throw new DomainException("Featured image asset not found.");
            if (asset.VirusScanStatus != VirusScanStatus.Clean)
                throw new DomainException("Featured image asset is not clean.");
        }

        return CountryContentRequest.SubmitNews(
            countryId, userId,
            body.TitleAr, body.TitleEn,
            body.ContentAr, body.ContentEn,
            body.TopicId, body.FeaturedImageAssetId,
            _clock);
    }

    private async Task<CountryContentRequest> SubmitEventAsync(
        CreateEventBody body,
        System.Guid countryId,
        System.Guid userId,
        CancellationToken ct)
    {
        var topics = await _db.Topics
            .Where(t => t.Id == body.TopicId)
            .ToListAsyncEither(ct).ConfigureAwait(false);
        if (topics.Count == 0)
            throw new DomainException("Topic not found.");

        if (body.StartsOn >= body.EndsOn)
            throw new DomainException("StartsOn must be before EndsOn.");

        if (body.FeaturedImageAssetId.HasValue)
        {
            var assets = await _db.AssetFiles
                .Where(a => a.Id == body.FeaturedImageAssetId.Value)
                .ToListAsyncEither(ct).ConfigureAwait(false);
            var asset = assets.FirstOrDefault();
            if (asset is null)
                throw new DomainException("Featured image asset not found.");
            if (asset.VirusScanStatus != VirusScanStatus.Clean)
                throw new DomainException("Featured image asset is not clean.");
        }

        return CountryContentRequest.SubmitEvent(
            countryId, userId,
            body.TitleAr, body.TitleEn,
            body.DescriptionAr, body.DescriptionEn,
            body.TopicId,
            body.StartsOn, body.EndsOn,
            body.LocationAr, body.LocationEn, body.OnlineMeetingUrl,
            _clock);
    }
}
