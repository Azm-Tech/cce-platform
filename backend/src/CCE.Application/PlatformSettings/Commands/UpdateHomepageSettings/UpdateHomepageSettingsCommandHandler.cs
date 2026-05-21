using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateHomepageSettings;

public sealed class UpdateHomepageSettingsCommandHandler
    : IRequestHandler<UpdateHomepageSettingsCommand, Response<HomepageSettingsDto>>
{
    private readonly IHomepageSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateHomepageSettingsCommandHandler(
        IHomepageSettingsRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<HomepageSettingsDto>> Handle(
        UpdateHomepageSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.HomepageSettingsNotFound<HomepageSettingsDto>();

        settings.UpdateContent(
            request.VideoUrl,
            request.ObjectiveAr,
            request.ObjectiveEn,
            request.CceConceptsAr,
            request.CceConceptsEn);

        var existing = _db.HomepageCountries
            .Where(hc => hc.HomepageSettingsId == settings.Id);
        _db.DeleteRange(existing);

        var order = 0;
        foreach (var countryId in request.ParticipatingCountryIds)
        {
            _db.Add(HomepageCountry.Create(settings.Id, countryId, order++));
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var countries = _db.HomepageCountries
            .Where(hc => hc.HomepageSettingsId == settings.Id)
            .OrderBy(hc => hc.OrderIndex)
            .Select(hc => new HomepageCountryDto(hc.Id, hc.CountryId, hc.OrderIndex))
            .ToList();

        return _msg.Ok(new HomepageSettingsDto(
            settings.Id,
            settings.VideoUrl,
            settings.ObjectiveAr,
            settings.ObjectiveEn,
            settings.CceConceptsAr,
            settings.CceConceptsEn,
            countries,
            Convert.ToBase64String(settings.RowVersion)), "SETTINGS_UPDATED");
    }
}
