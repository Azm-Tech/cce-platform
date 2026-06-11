using System.Collections.Generic;
using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListExpertLeaderboard;

/// <summary>
/// Builds the experts leaderboard (§A.1 read path). Loads the (small) set of expert profiles,
/// counts each expert's published posts and replies in SQL, then ranks by
/// <c>Score = PostCount + ReplyCount</c> in memory and paginates.
/// </summary>
public sealed class ListExpertLeaderboardQueryHandler
    : IRequestHandler<ListExpertLeaderboardQuery, Response<PagedResult<ExpertLeaderboardEntryDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public ListExpertLeaderboardQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PagedResult<ExpertLeaderboardEntryDto>>> Handle(
        ListExpertLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var page = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, PaginationExtensions.MaxPageSize);

        var profiles = await _db.ExpertProfiles
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        if (profiles.Count == 0)
        {
            return _msg.Ok(
                new PagedResult<ExpertLeaderboardEntryDto>(
                    System.Array.Empty<ExpertLeaderboardEntryDto>(), page, pageSize, 0),
                "ITEMS_LISTED");
        }

        var userIds = profiles.Select(p => p.UserId).Distinct().ToList();

        var users = (await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.JobTitle, u.OrganizationName, u.AvatarUrl })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(u => u.Id);

        var postCounts = (await _db.Posts
            .Where(p => p.Status == PostStatus.Published && userIds.Contains(p.AuthorId))
            .GroupBy(p => p.AuthorId)
            .Select(g => new { AuthorId = g.Key, Count = g.Count() })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(x => x.AuthorId, x => x.Count);

        var replyCounts = (await _db.PostReplies
            .Where(r => userIds.Contains(r.AuthorId))
            .GroupBy(r => r.AuthorId)
            .Select(g => new { AuthorId = g.Key, Count = g.Count() })
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false))
            .ToDictionary(x => x.AuthorId, x => x.Count);

        var ranked = profiles
            .Where(p => users.ContainsKey(p.UserId))
            .Select(p =>
            {
                var u = users[p.UserId];
                var postCount = postCounts.GetValueOrDefault(p.UserId, 0);
                var replyCount = replyCounts.GetValueOrDefault(p.UserId, 0);
                return new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.JobTitle,
                    u.OrganizationName,
                    u.AvatarUrl,
                    Tags = (IReadOnlyList<string>)(p.ExpertiseTags?.ToList() ?? new List<string>()),
                    PostCount = postCount,
                    ReplyCount = replyCount,
                    Score = postCount + replyCount,
                };
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.LastName)
            .ToList();

        var items = ranked
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select((x, i) => new ExpertLeaderboardEntryDto(
                x.Id,
                x.FirstName,
                x.LastName,
                x.JobTitle,
                x.OrganizationName,
                x.AvatarUrl,
                x.Tags,
                x.PostCount,
                x.ReplyCount,
                x.Score,
                (page - 1) * pageSize + i + 1))
            .ToList();

        return _msg.Ok(
            new PagedResult<ExpertLeaderboardEntryDto>(items, page, pageSize, ranked.Count),
            "ITEMS_LISTED");
    }
}
