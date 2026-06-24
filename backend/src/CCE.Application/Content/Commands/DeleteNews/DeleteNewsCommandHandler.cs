using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.DeleteNews;

public sealed class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, Response<VoidData>>
{
    private readonly IRepository<News, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public DeleteNewsCommandHandler(
        IRepository<News, System.Guid> repo,
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<VoidData>> Handle(DeleteNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (news is null)
            return _messages.NewsNotFound<VoidData>();

        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _messages.NotAuthenticated<VoidData>();

        news.SoftDelete(userId.Value, _clock);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(MessageKeys.Content.CONTENT_DELETED);
    }
}
