using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicPostsInTopic;

public sealed class ListPublicPostsInTopicQueryHandler
    : IRequestHandler<ListPublicPostsInTopicQuery, PagedResult<PublicPostDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicPostsInTopicQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicPostDto>> Handle(
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
            return new PagedResult<PublicPostDto>(
                System.Array.Empty<PublicPostDto>(),
                paged.Page,
                paged.PageSize,
                paged.Total);
        }

        var authorIds = items.Select(p => p.AuthorId).Distinct().ToList();
        var postIds = items.Select(p => p.Id).ToList();

        var authorNames = (await _db.Users
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(a => a.Id, a => a.Name);

        var attachmentsByPost = (await _db.PostAttachments
            .Where(a => postIds.Contains(a.PostId))
            .Select(a => new { a.PostId, a.AssetFileId })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .GroupBy(a => a.PostId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.AssetFileId).ToList());

        var dtos = items.Select(p => MapToDto(
            p,
            authorNames.GetValueOrDefault(p.AuthorId),
            attachmentsByPost.GetValueOrDefault(p.Id, new List<System.Guid>()))).ToList();

        return new PagedResult<PublicPostDto>(dtos, paged.Page, paged.PageSize, paged.Total);
    }

    internal static PublicPostDto MapToDto(
        Post p,
        string? authorName,
        System.Collections.Generic.IReadOnlyList<System.Guid> attachmentIds) => new(
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
        p.CreatedOn);
}
