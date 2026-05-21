using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteGlossaryEntry;

public sealed class DeleteGlossaryEntryCommandHandler
    : IRequestHandler<DeleteGlossaryEntryCommand, Response<VoidData>>
{
    private readonly IGlossaryEntryRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteGlossaryEntryCommandHandler(
        IGlossaryEntryRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteGlossaryEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return _msg.GlossaryEntryNotFound<VoidData>();

        _db.Delete(entry);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok("CONTENT_DELETED");
    }
}
