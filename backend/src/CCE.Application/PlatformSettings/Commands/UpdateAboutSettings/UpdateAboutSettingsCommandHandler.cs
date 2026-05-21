using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateAboutSettings;

public sealed class UpdateAboutSettingsCommandHandler
    : IRequestHandler<UpdateAboutSettingsCommand, Response<AboutSettingsDto>>
{
    private readonly IAboutSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateAboutSettingsCommandHandler(
        IAboutSettingsRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<AboutSettingsDto>> Handle(
        UpdateAboutSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
            return _msg.AboutSettingsNotFound<AboutSettingsDto>();

        settings.UpdateContent(request.DescriptionAr, request.DescriptionEn, request.HowToUseVideoUrl);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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
            settings.DescriptionAr,
            settings.DescriptionEn,
            settings.HowToUseVideoUrl,
            glossary.Select(e => new GlossaryEntryDto(
                e.Id, e.TermAr, e.TermEn, e.DefinitionAr, e.DefinitionEn, e.OrderIndex)).ToList(),
            partners.Select(p => new KnowledgePartnerDto(
                p.Id, p.NameAr, p.NameEn, p.LogoUrl, p.WebsiteUrl,
                p.DescriptionAr, p.DescriptionEn, p.OrderIndex)).ToList(),
            Convert.ToBase64String(settings.RowVersion)), "SETTINGS_UPDATED");
    }
}
