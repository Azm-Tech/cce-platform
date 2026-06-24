using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateHomepageSettings;

public sealed class UpdateHomepageSettingsCommandHandler
    : IRequestHandler<UpdateHomepageSettingsCommand, Response<System.Guid>>
{
    private readonly IHomepageSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public UpdateHomepageSettingsCommandHandler(
        IHomepageSettingsRepository repo,
        ICceDbContext db,
        MessageFactory msg,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Response<System.Guid>> Handle(
        UpdateHomepageSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.HomepageSettingsNotFound<System.Guid>();

        var userId = _currentUser.GetUserId()
            ?? throw new DomainException("User identity required.");
        var objective = LocalizedText.Create(request.ObjectiveAr, request.ObjectiveEn);

        settings.UpdateContent(
            request.VideoUrl,
            objective,
            request.CceConceptsAr,
            request.CceConceptsEn,
            userId,
            _clock);

        settings.SyncCountries(request.ParticipatingCountryIds, userId, _clock);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(settings.Id, MessageKeys.PlatformSettings.SETTINGS_UPDATED);
    }
}
