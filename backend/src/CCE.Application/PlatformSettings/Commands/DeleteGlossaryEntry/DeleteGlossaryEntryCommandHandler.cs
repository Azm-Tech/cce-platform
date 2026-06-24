using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteGlossaryEntry;

public sealed class DeleteGlossaryEntryCommandHandler
    : IRequestHandler<DeleteGlossaryEntryCommand, Response<VoidData>>
{
    private readonly IAboutSettingsRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteGlossaryEntryCommandHandler(
        IAboutSettingsRepository repo,
        ICceDbContext db,
        MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteGlossaryEntryCommand request, CancellationToken cancellationToken)
    {
        var about = await _repo.GetAsync(cancellationToken).ConfigureAwait(false);
        if (about is null)
            return _msg.AboutSettingsNotFound<VoidData>();

        var entry = about.GlossaryEntries.FirstOrDefault(e => e.Id == request.Id);
        if (entry is null)
            return _msg.GlossaryEntryNotFound<VoidData>();

        about.RemoveGlossaryEntry(entry);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(MessageKeys.Content.CONTENT_DELETED);
    }
}
