using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicPostReplies;

public sealed class ListPublicPostRepliesQueryHandler
    : IRequestHandler<ListPublicPostRepliesQuery, PagedResult<PublicPostReplyDto>>
{
    private readonly ICceDbContext _db;

    public ListPublicPostRepliesQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicPostReplyDto>> Handle(
        ListPublicPostRepliesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.PostReplies
            .Where(r => r.PostId == request.PostId)
            .OrderBy(r => r.CreatedOn)
            .Select(r => MapToDto(r));

        return await query
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static PublicPostReplyDto MapToDto(PostReply r) => new(
        r.Id,
        r.PostId,
        r.AuthorId,
        r.Content,
        r.Locale,
        r.ParentReplyId,
        r.IsByExpert,
        r.CreatedOn);
}
