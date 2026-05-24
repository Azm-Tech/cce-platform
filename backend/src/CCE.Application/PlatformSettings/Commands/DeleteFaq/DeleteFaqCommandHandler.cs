using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.PlatformSettings.Commands.DeleteFaq;

public sealed class DeleteFaqCommandHandler
    : IRequestHandler<DeleteFaqCommand, Response<VoidData>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public DeleteFaqCommandHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(
        DeleteFaqCommand request, CancellationToken cancellationToken)
    {
        var faq = await _db.Faqs
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken)
            .ConfigureAwait(false);

        if (faq is null)
            return _msg.FaqNotFound<VoidData>();

        _db.Delete(faq);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok("CONTENT_DELETED");
    }
}
