using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Sanitization;
using CCE.Application.Messages;

using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Commands.UpdateDraft;

public sealed class UpdateDraftCommandHandler
    : IRequestHandler<UpdateDraftCommand, Response<VoidData>>
{
    private readonly IPostRepository _repo;
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IHtmlSanitizer _sanitizer;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _msg;

    public UpdateDraftCommandHandler(
        IPostRepository repo, ICceDbContext db, ICurrentUserAccessor currentUser,
        IHtmlSanitizer sanitizer, ISystemClock clock, MessageFactory msg)
    {
        _repo = repo;
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
        _clock = clock;
        _msg = msg;
    }

    public async Task<Response<VoidData>> Handle(UpdateDraftCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == Guid.Empty) return _msg.Unauthorized<VoidData>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var post = await _repo.GetAsync(request.PostId, cancellationToken).ConfigureAwait(false);
        if (post is null) return _msg.NotFound<VoidData>(MessageKeys.Community.POST_NOT_FOUND);
        if (post.AuthorId != userId.Value) return _msg.Forbidden<VoidData>(MessageKeys.General.FORBIDDEN);
        if (post.Status != PostStatus.Draft)
            return _msg.BusinessRule<VoidData>(MessageKeys.Community.POST_ALREADY_PUBLISHED);

        var sanitized = request.Content is null ? null : _sanitizer.Sanitize(request.Content);
        post.UpdateDraft(request.Title, sanitized, userId.Value, _clock);

        var tags = await _repo.GetTagsAsync(request.TagIds, cancellationToken).ConfigureAwait(false);
        post.SetTags(tags);

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return _msg.Ok(MessageKeys.Community.POST_DRAFT_SAVED);
    }
}
