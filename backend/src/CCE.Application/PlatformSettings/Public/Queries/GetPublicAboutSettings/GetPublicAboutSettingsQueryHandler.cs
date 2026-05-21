using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Public.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Public.Queries.GetPublicAboutSettings;

public sealed class GetPublicAboutSettingsQueryHandler
    : IRequestHandler<GetPublicAboutSettingsQuery, Response<PublicAboutSettingsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicAboutSettingsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicAboutSettingsDto>> Handle(
        GetPublicAboutSettingsQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.AboutSettings.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var settings = list.FirstOrDefault();
        if (settings is null)
            return _msg.AboutSettingsNotFound<PublicAboutSettingsDto>();

        var glossary = await _db.GlossaryEntries
            .Where(e => e.AboutSettingsId == settings.Id)
            .OrderBy(e => e.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var partners = await _db.KnowledgePartners
            .Where(p => p.AboutSettingsId == settings.Id)
            .OrderBy(p => p.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(new PublicAboutSettingsDto(
            settings.DescriptionAr,
            settings.DescriptionEn,
            settings.HowToUseVideoUrl,
            glossary.Select(e => new PublicGlossaryEntryDto(e.TermAr, e.TermEn, e.DefinitionAr, e.DefinitionEn)).ToList(),
            partners.Select(p => new PublicKnowledgePartnerDto(
                p.NameAr, p.NameEn, p.LogoUrl, p.WebsiteUrl,
                p.DescriptionAr, p.DescriptionEn)).ToList()), "ITEMS_LISTED");
    }
}
