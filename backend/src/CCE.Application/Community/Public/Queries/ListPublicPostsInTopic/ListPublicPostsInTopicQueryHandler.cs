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
            .Where(p => p.TopicId == request.TopicId)
            .OrderByDescending(p => p.CreatedOn)
            .Select(p => MapToDto(p));

        return await query
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static PublicPostDto MapToDto(Post p) => new(
        p.Id,
        p.TopicId,
        p.AuthorId,
        p.Content,
        p.Locale,
        p.IsAnswerable,
        p.AnsweredReplyId,
        p.CreatedOn);
}
