using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.ListPublicPostReplies;

public sealed class ListPublicPostRepliesQueryHandler
    : IRequestHandler<ListPublicPostRepliesQuery, Response<PagedResult<PublicPostReplyDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListPublicPostRepliesQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<PublicPostReplyDto>>> Handle(
        ListPublicPostRepliesQuery request,
        CancellationToken cancellationToken)
    {
        var paged = await _db.PostReplies
            .Where(r => r.PostId == request.PostId && r.ParentReplyId == null)
            .OrderByDescending(r => r.Score)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var authorIds = paged.Items.Select(r => r.AuthorId).Distinct().ToList();
        var authorMap = await LoadAuthorMapAsync(_db, authorIds, cancellationToken).ConfigureAwait(false);

        var dtos = paged.Items.Select(r => MapToDto(r, authorMap)).ToList();

        return _msg.Ok(
            new PagedResult<PublicPostReplyDto>(dtos, paged.Page, paged.PageSize, paged.Total),
            MessageKeys.General.ITEMS_LISTED);
    }

    internal static async Task<System.Collections.Generic.Dictionary<System.Guid, (string Name, string? AvatarUrl)>> LoadAuthorMapAsync(
        ICceDbContext db,
        System.Collections.Generic.List<System.Guid> authorIds,
        CancellationToken ct)
    {
        if (authorIds.Count == 0)
            return new();

        var users = await db.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName, u.AvatarUrl })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        return users.ToDictionary(
            u => u.Id,
            u =>
            {
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                var name = string.IsNullOrEmpty(fullName) ? u.UserName ?? string.Empty : fullName;
                return (name, u.AvatarUrl);
            });
    }

    internal static PublicPostReplyDto MapToDto(
        PostReply r,
        System.Collections.Generic.Dictionary<System.Guid, (string Name, string? AvatarUrl)> authorMap)
    {
        var author = authorMap.GetValueOrDefault(r.AuthorId);
        return new(
            r.Id,
            r.PostId,
            r.AuthorId,
            r.Content,
            r.Locale,
            r.ParentReplyId,
            r.IsByExpert,
            r.Depth,
            r.ChildCount,
            r.UpvoteCount,
            r.CreatedOn,
            author.Name,
            author.AvatarUrl);
    }
}
