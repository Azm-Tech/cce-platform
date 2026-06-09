using System.Collections.Generic;
using CCE.Application.Common;
using CCE.Application.Common.Caching;
using MediatR;

namespace CCE.Application.Community.Commands.CreateReply;

/// <summary>US029 — reply to a post (optionally nested under a parent reply) with @mentions.</summary>
public sealed record CreateReplyCommand(
    Guid PostId,
    string Content,
    string Locale,
    Guid? ParentReplyId,
    IReadOnlyList<Guid> MentionedUserIds) : IRequest<Response<Guid>>, ICacheInvalidatingRequest
{
    public IReadOnlyCollection<string> CacheRegionsToEvict { get; } = [CacheRegions.Posts];
}
