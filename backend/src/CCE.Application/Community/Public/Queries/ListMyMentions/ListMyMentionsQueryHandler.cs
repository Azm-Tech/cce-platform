using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.ListMyMentions;

public sealed class ListMyMentionsQueryHandler
    : IRequestHandler<ListMyMentionsQuery, Response<PagedResult<MyMentionDto>>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public ListMyMentionsQueryHandler(ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<PagedResult<MyMentionDto>>> Handle(
        ListMyMentionsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == System.Guid.Empty)
            return _msg.Unauthorized<PagedResult<MyMentionDto>>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var paged = await _db.Mentions
            .Where(m => m.MentionedUserId == userId.Value)
            .OrderByDescending(m => m.CreatedOn)
            .Join(_db.Users, m => m.MentionedByUserId, u => u.Id,
                (m, u) => new MyMentionDto(
                    m.Id,
                    m.SourceType,
                    m.SourceId,
                    m.PostId,
                    m.CommunityId,
                    m.MentionedByUserId,
                    u.FirstName + " " + u.LastName,
                    u.AvatarUrl,
                    m.Snippet,
                    m.CreatedOn))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
