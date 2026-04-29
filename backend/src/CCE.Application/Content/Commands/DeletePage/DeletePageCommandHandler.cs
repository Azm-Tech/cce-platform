using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeletePage;

public sealed class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Unit>
{
    private readonly IPageService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public DeletePageCommandHandler(IPageService service, ICurrentUserAccessor currentUser, ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeletePageCommand request, CancellationToken cancellationToken)
    {
        var page = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (page is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Page {request.Id} not found.");
        }

        var deletedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot delete page from a request without a user identity.");

        page.SoftDelete(deletedById, _clock);
        await _service.UpdateAsync(page, page.RowVersion, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
