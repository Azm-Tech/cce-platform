using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Application.PlatformSettings.Public.Dtos;
using CCE.Domain.Content;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicHomepage;

public sealed class GetPublicHomepageQueryHandler
    : IRequestHandler<GetPublicHomepageQuery, Response<PublicHomepageDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicHomepageQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicHomepageDto>> Handle(
        GetPublicHomepageQuery request, CancellationToken cancellationToken)
    {
        var settingsList = await _db.HomepageSettings.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var settings = settingsList.FirstOrDefault();
        if (settings is null)
            return _msg.HomepageSettingsNotFound<PublicHomepageDto>();

        var countries = await (
            from hc in _db.HomepageCountries
            join c in _db.Countries on hc.CountryId equals c.Id
            where hc.HomepageSettingsId == settings.Id
            orderby hc.OrderIndex
            select new PublicHomepageCountryDto(c.Id, c.IsoAlpha3, c.NameAr, c.NameEn, c.FlagUrl, hc.OrderIndex)
        ).ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var sections = await _db.HomepageSections
            .Where(s => s.IsActive)
            .OrderBy(s => s.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new PublicHomepageDto(
            settings.VideoUrl,
            new LocalizedTextDto(settings.Objective.Ar, settings.Objective.En),
            settings.CceConceptsAr,
            settings.CceConceptsEn,
            countries,
            sections.Select(s => new PublicHomepageSectionDto(
                s.Id, s.SectionType, s.OrderIndex, s.ContentAr, s.ContentEn)).ToList()), "ITEMS_LISTED");
    }
}
