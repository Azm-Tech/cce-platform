using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.ListMyReplies;

public sealed class ListMyRepliesQueryHandler
    : IRequestHandler<ListMyRepliesQuery, Response<PagedResult<MyReplyItemDto>>>
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly MessageFactory _msg;

    public ListMyRepliesQueryHandler(ICceDbContext db, ICurrentUserAccessor currentUser, MessageFactory msg)
    {
        _db = db;
        _currentUser = currentUser;
        _msg = msg;
    }

    public async Task<Response<PagedResult<MyReplyItemDto>>> Handle(
        ListMyRepliesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();
        if (userId is null || userId == System.Guid.Empty)
            return _msg.Unauthorized<PagedResult<MyReplyItemDto>>(MessageKeys.Identity.NOT_AUTHENTICATED);

        var user = await _db.Users
            .Where(u => u.Id == userId.Value)
            .Select(u => new { u.FirstName, u.LastName, u.UserName, u.CommentsCount })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
            return _msg.NotFound<PagedResult<MyReplyItemDto>>(MessageKeys.Identity.USER_NOT_FOUND);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var authorName = string.IsNullOrEmpty(fullName) ? user.UserName ?? string.Empty : fullName;

        var page     = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, PaginationExtensions.MaxPageSize);

        var baseQuery =
            from r in _db.PostReplies
            join p in _db.Posts on r.PostId equals p.Id
            where r.AuthorId == userId.Value && !r.IsDeleted
            orderby r.CreatedOn descending
            select new MyReplyItemDto(
                r.Id, r.PostId, p.Title ?? string.Empty,
                userId.Value, authorName,
                r.Content, r.CreatedOn, r.UpvoteCount, r.DownvoteCount);

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var paged = new PagedResult<MyReplyItemDto>(items, page, pageSize, user.CommentsCount);
        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
