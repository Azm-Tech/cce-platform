using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Messages;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeletePage;

public sealed class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Response<VoidData>>
{
    private readonly IPageRepository _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public DeletePageCommandHandler(IPageRepository service, ICurrentUserAccessor currentUser, ISystemClock clock, MessageFactory msg)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            return _msg.NotFound<VoidData>(MessageKeys.Content.PAGE_NOT_FOUND);
        }

        var deletedById = _currentUser.GetUserId();
        if (deletedById is null)
        {
            return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);
        }

        page.SoftDelete(deletedById.Value, _clock);
        await _service.UpdateAsync(page, page.RowVersion, cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Content.CONTENT_DELETED);
    }
}
