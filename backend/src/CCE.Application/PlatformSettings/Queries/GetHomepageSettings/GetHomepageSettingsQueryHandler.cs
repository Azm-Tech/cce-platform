using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetHomepageSettings;

public sealed class GetHomepageSettingsQueryHandler
    : IRequestHandler<GetHomepageSettingsQuery, Response<HomepageSettingsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetHomepageSettingsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<HomepageSettingsDto>> Handle(
        GetHomepageSettingsQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.HomepageSettings.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var settings = list.FirstOrDefault();
        if (settings is null)
            return _msg.HomepageSettingsNotFound<HomepageSettingsDto>();

        var countries = await _db.HomepageCountries
            .Where(hc => hc.HomepageSettingsId == settings.Id)
            .OrderBy(hc => hc.OrderIndex)
            .Select(hc => new HomepageCountryDto(hc.Id, hc.CountryId, hc.OrderIndex))
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new HomepageSettingsDto(
            settings.Id,
            settings.VideoUrl,
            settings.ObjectiveAr,
            settings.ObjectiveEn,
            settings.CceConceptsAr,
            settings.CceConceptsEn,
            countries,
            Convert.ToBase64String(settings.RowVersion)), "ITEMS_LISTED");
    }
}
