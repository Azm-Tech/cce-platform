using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.CreateGlossaryEntry;

public sealed class CreateGlossaryEntryCommandHandler
    : IRequestHandler<CreateGlossaryEntryCommand, Response<GlossaryEntryDto>>
{
    private readonly IAboutSettingsRepository _aboutRepo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public CreateGlossaryEntryCommandHandler(
        IAboutSettingsRepository aboutRepo, ICceDbContext db, MessageFactory msg)
    {
        _aboutRepo = aboutRepo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<GlossaryEntryDto>> Handle(
        CreateGlossaryEntryCommand request, CancellationToken cancellationToken)
    {
        var about = await _aboutRepo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (about is null)
            return _msg.AboutSettingsNotFound<GlossaryEntryDto>();

        var maxOrder = await _db.GlossaryEntries
            .Where(e => e.AboutSettingsId == about.Id)
            .Select(e => (int?)e.OrderIndex)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var nextOrder = (maxOrder.FirstOrDefault() ?? -1) + 1;

        var entry = GlossaryEntry.Create(
            about.Id, request.TermAr, request.TermEn,
            request.DefinitionAr, request.DefinitionEn, nextOrder);

        _db.Add(entry);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new GlossaryEntryDto(
            entry.Id, entry.TermAr, entry.TermEn,
            entry.DefinitionAr, entry.DefinitionEn, entry.OrderIndex), "CONTENT_CREATED");
    }
}
