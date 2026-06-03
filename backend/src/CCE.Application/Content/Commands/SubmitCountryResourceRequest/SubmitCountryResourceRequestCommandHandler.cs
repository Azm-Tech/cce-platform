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

namespace CCE.Application.Content.Commands.SubmitCountryResourceRequest;

public sealed class SubmitCountryResourceRequestCommandHandler
    : IRequestHandler<SubmitCountryResourceRequestCommand, Response<System.Guid>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICountryScopeAccessor _scope;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;
    private readonly INotificationMessageDispatcher _dispatcher;

    public SubmitCountryResourceRequestCommandHandler(
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
        SubmitCountryResourceRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Scope guard — state reps can only submit for their own country
        var authorizedIds = await _scope.GetAuthorizedCountryIdsAsync(cancellationToken).ConfigureAwait(false);
        if (authorizedIds is not null && !authorizedIds.Contains(request.CountryId))
            return _messages.CountryScopeForbidden<System.Guid>();

        // Asset must exist and be clean
        var assets = await _db.AssetFiles
            .Where(a => a.Id == request.AssetFileId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var asset = assets.FirstOrDefault();
        if (asset is null)
            return _messages.AssetNotFound<System.Guid>();
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            return _messages.AssetNotClean<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot submit without a user identity.");

        var contentRequest = CountryContentRequest.SubmitResource(
            request.CountryId, userId,
            request.TitleAr, request.TitleEn,
            request.DescriptionAr, request.DescriptionEn,
            request.ResourceType, request.AssetFileId,
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
