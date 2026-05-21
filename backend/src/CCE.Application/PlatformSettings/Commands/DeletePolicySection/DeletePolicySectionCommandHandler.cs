using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeletePolicySection;

public sealed class DeletePolicySectionCommandHandler
    : IRequestHandler<DeletePolicySectionCommand, Response<VoidData>>
{
    private readonly IPolicySectionRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeletePolicySectionCommandHandler(
        IPolicySectionRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeletePolicySectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (section is null)
            return _msg.PolicySectionNotFound<VoidData>();

        _db.Delete(section);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok("CONTENT_DELETED");
    }
}
