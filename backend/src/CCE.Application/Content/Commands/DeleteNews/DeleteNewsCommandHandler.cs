using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteNews;

public sealed class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, Unit>
{
    private readonly INewsService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public DeleteNewsCommandHandler(INewsService service, ICurrentUserAccessor currentUser, ISystemClock clock)
    {
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(DeleteNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (news is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"News {request.Id} not found.");
        }

        var deletedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot delete news from a request without a user identity.");

        news.SoftDelete(deletedById, _clock);
        await _service.UpdateAsync(news, news.RowVersion, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
