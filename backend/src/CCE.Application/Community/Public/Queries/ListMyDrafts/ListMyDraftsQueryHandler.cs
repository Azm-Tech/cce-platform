using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListMyDrafts;

/// <summary>Read path (§A.1): context projection of the caller's drafts. Returns Response.</summary>
public sealed class ListMyDraftsQueryHandler
    : IRequestHandler<ListMyDraftsQuery, Response<PagedResult<MyDraftDto>>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public ListMyDraftsQueryHandler(ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<PagedResult<MyDraftDto>>> Handle(
        ListMyDraftsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == System.Guid.Empty)
            return _msg.Unauthorized<PagedResult<MyDraftDto>>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var paged = await _db.Posts
            .Where(p => p.AuthorId == userId.Value && p.Status == PostStatus.Draft)
            .OrderByDescending(p => p.CreatedOn)
            .Select(p => new MyDraftDto(
                p.Id, p.TopicId, p.Type, p.Title, p.Content, p.Locale, p.CreatedOn, p.LastModifiedOn))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
