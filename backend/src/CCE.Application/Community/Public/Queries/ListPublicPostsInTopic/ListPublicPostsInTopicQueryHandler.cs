using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;

public sealed class ListPublicPostsInTopicQueryHandler
    : IRequestHandler<ListPublicPostsInTopicQuery, Response<PagedResult<PublicPostDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;
    private readonly ISystemClock _clock;

    public ListPublicPostsInTopicQueryHandler(ICceDbContext db, MessageFactory msg, ISystemClock clock)
    {
        _db = db;
        _msg = msg;
        _clock = clock;
    }

    public async Task<Response<PagedResult<PublicPostDto>>> Handle(
        ListPublicPostsInTopicQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Posts
            .Where(p => p.TopicId == request.TopicId && p.Status == PostStatus.Published)
            .OrderByDescending(p => p.Score);

        var paged = await query
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items.ToList();
        if (items.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<PublicPostDto>(
                    System.Array.Empty<PublicPostDto>(), paged.Page, paged.PageSize, paged.Total),
                MessageKeys.General.ITEMS_LISTED);
        }

        var authorIds = items.Select(p => p.AuthorId).Distinct().ToList();
        var postIds = items.Select(p => p.Id).ToList();

        var authorNames = (await _db.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.UserName })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(a => a.Id, a =>
            {
                var fullName = $"{a.FirstName} {a.LastName}".Trim();
                return string.IsNullOrEmpty(fullName) ? a.UserName ?? string.Empty : fullName;
            });

        var attachmentsByPost = (await _db.PostAttachments
            .Where(a => postIds.Contains(a.PostId))
            .Select(a => new { a.PostId, a.MediaFileId })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .GroupBy(a => a.PostId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.MediaFileId).ToList());

        // Poll data — batch fetch; UserVoted populated when caller is authenticated.
        var pollPostIds  = items.Where(p => p.Type == PostType.Poll).Select(p => p.Id).ToList();
        var pollsByPostId = await PollHydrator.FetchAsync(_db, _clock, pollPostIds, request.UserId, cancellationToken)
            .ConfigureAwait(false);

        var dtos = items.Select(p => MapToDto(
            p,
            authorNames.GetValueOrDefault(p.AuthorId),
            attachmentsByPost.GetValueOrDefault(p.Id, new List<System.Guid>()),
            pollsByPostId.GetValueOrDefault(p.Id))).ToList();

        return _msg.Ok(
            new PagedResult<PublicPostDto>(dtos, paged.Page, paged.PageSize, paged.Total),
            MessageKeys.General.ITEMS_LISTED);
    }

    internal static PublicPostDto MapToDto(
        Post p,
        string? authorName,
        System.Collections.Generic.IReadOnlyList<System.Guid> attachmentIds,
        PollSummaryDto? poll = null) => new(
        p.Id,
        p.CommunityId,
        p.TopicId,
        p.AuthorId,
        authorName,
        p.Type,
        p.Title,
        p.Content,
        p.Locale,
        p.IsAnswerable,
        p.AnsweredReplyId,
        p.UpvoteCount,
        p.DownvoteCount,
        p.CommentsCount,
        attachmentIds,
        p.CreatedOn,
        poll);
}
