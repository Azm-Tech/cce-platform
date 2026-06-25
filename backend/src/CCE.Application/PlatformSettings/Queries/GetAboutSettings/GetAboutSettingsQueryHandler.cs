using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Queries.GetAboutSettings;

public sealed class GetAboutSettingsQueryHandler
    : IRequestHandler<GetAboutSettingsQuery, Response<AboutSettingsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetAboutSettingsQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<AboutSettingsDto>> Handle(
        GetAboutSettingsQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.AboutSettings.ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var settings = list.FirstOrDefault();
        if (settings is null)
            return _msg.NotFound<AboutSettingsDto>(MessageKeys.PlatformSettings.ABOUT_SETTINGS_NOT_FOUND);

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

        return _msg.Ok(new AboutSettingsDto(
            settings.Id,
            new LocalizedTextDto(settings.Description.Ar, settings.Description.En),
            settings.HowToUseVideoUrl,
            glossary.Select(e => new GlossaryEntryDto(
                e.Id,
                new LocalizedTextDto(e.Term.Ar, e.Term.En),
                new LocalizedTextDto(e.Definition.Ar, e.Definition.En),
                e.OrderIndex)).ToList(),
            partners.Select(p => new KnowledgePartnerDto(
                p.Id,
                new LocalizedTextDto(p.Name.Ar, p.Name.En),
                p.LogoUrl,
                p.WebsiteUrl,
                p.Description is null ? null : new LocalizedTextDto(p.Description.Ar, p.Description.En),
                p.OrderIndex)).ToList()), MessageKeys.General.ITEMS_LISTED);
    }
}
