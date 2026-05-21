using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Application.PlatformSettings.Dtos;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.UpdateGlossaryEntry;

public sealed class UpdateGlossaryEntryCommandHandler
    : IRequestHandler<UpdateGlossaryEntryCommand, Response<GlossaryEntryDto>>
{
    private readonly IGlossaryEntryRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpdateGlossaryEntryCommandHandler(
        IGlossaryEntryRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<GlossaryEntryDto>> Handle(
        UpdateGlossaryEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return _msg.GlossaryEntryNotFound<GlossaryEntryDto>();

        entry.UpdateContent(
            request.TermAr, request.TermEn,
            request.DefinitionAr, request.DefinitionEn);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new GlossaryEntryDto(
            entry.Id, entry.TermAr, entry.TermEn,
            entry.DefinitionAr, entry.DefinitionEn, entry.OrderIndex), "CONTENT_UPDATED");
    }
}
