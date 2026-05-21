using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.PlatformSettings;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteKnowledgePartner;

public sealed class DeleteKnowledgePartnerCommandHandler
    : IRequestHandler<DeleteKnowledgePartnerCommand, Response<VoidData>>
{
    private readonly IKnowledgePartnerRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteKnowledgePartnerCommandHandler(
        IKnowledgePartnerRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteKnowledgePartnerCommand request, CancellationToken cancellationToken)
    {
        var partner = await _repo.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (partner is null)
            return _msg.KnowledgePartnerNotFound<VoidData>();

        _db.Delete(partner);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok("CONTENT_DELETED");
    }
}
