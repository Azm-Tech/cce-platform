using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
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
            new LocalizedTextDto(settings.Description.Ar, settings.Description.En),
            settings.HowToUseVideoUrl,
            glossary.Select(e => new PublicGlossaryEntryDto(
                new LocalizedTextDto(e.Term.Ar, e.Term.En),
                new LocalizedTextDto(e.Definition.Ar, e.Definition.En))).ToList(),
            partners.Select(p => new PublicKnowledgePartnerDto(
                new LocalizedTextDto(p.Name.Ar, p.Name.En),
                p.LogoUrl,
                p.WebsiteUrl,
                p.Description is null ? null : new LocalizedTextDto(p.Description.Ar, p.Description.En))).ToList()), "ITEMS_LISTED");
    }
}
