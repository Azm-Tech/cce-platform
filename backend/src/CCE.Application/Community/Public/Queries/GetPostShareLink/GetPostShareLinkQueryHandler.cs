using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;

using CCE.Domain.Community;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Community.Public.Queries.GetPostShareLink;

/// <summary>US025 — returns a shareable relative link for a published post.</summary>
public sealed class GetPostShareLinkQueryHandler
    : IRequestHandler<GetPostShareLinkQuery, Response<PostShareLinkDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPostShareLinkQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PostShareLinkDto>> Handle(
        GetPostShareLinkQuery request, CancellationToken cancellationToken)
    {
        var exists = await _db.Posts
            .AnyAsync(p => p.Id == request.PostId && p.Status == PostStatus.Published, cancellationToken)
            .ConfigureAwait(false);
        if (!exists) return _msg.NotFound<PostShareLinkDto>(MessageKeys.Community.POST_NOT_FOUND);

        var dto = new PostShareLinkDto(request.PostId, $"/community/posts/{request.PostId}");
        return _msg.Ok(dto, MessageKeys.General.ITEMS_LISTED);
    }
}
