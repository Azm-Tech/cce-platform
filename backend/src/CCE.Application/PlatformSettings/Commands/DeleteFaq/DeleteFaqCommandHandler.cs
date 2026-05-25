using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.PlatformSettings.Commands.DeleteFaq;

public sealed class DeleteFaqCommandHandler
    : IRequestHandler<DeleteFaqCommand, Response<VoidData>>
{
    private readonly IFaqRepository _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteFaqCommandHandler(IFaqRepository repo, ICceDbContext db, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _repo.GetByIdAsync(request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (faq is null)
            return _msg.FaqNotFound<VoidData>();

        _repo.Delete(faq);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok("CONTENT_DELETED");
    }
}
