using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;

using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.DeleteDraft;

public sealed class DeleteDraftCommandHandler
    : IRequestHandler<DeleteDraftCommand, Response<VoidData>>
{
    private readonly IPostRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public DeleteDraftCommandHandler(
        IPostRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(DeleteDraftCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.NotAuthenticated<VoidData>();

        var post = await _repo.GetAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null) return _msg.NotFound<VoidData>(MessageKeys.Community.POST_NOT_FOUND);
        if (post.AuthorId != userId.Value) return _msg.Forbidden<VoidData>(MessageKeys.General.FORBIDDEN);
        if (post.Status != PostStatus.Draft)
            return _msg.BusinessRule<VoidData>(MessageKeys.Community.POST_ALREADY_PUBLISHED);

        _repo.Remove(post);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Community.DRAFT_DELETED);
    }
}
