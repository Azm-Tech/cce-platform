using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

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
            .Select(m => new MyMentionDto(m.Id, m.SourceType, m.SourceId, m.MentionedByUserId, m.CreatedOn))
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        return _msg.Ok(paged, MessageKeys.General.ITEMS_LISTED);
    }
}
